using System;
using System.Text;
using System.Threading.Tasks;
using NuBus.Util;
using RabbitMQ.Client;
using Autofac;

namespace NuBus.Adapter
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;
    using Extension;
    using RabbitMQ.Client.Events;

    public class RabbitMQAdapter : IBusAdapter, IDisposable
    {
        protected bool _isStarted;
        public bool IsStarted
        {
            get { return _isStarted; }
            protected set { _isStarted = value; }
        }

        protected string _hostname;
        protected string _username;
        protected string _password;

        protected IContainer _locator;
        protected IConnection _connection;
        protected IModel _channel;
        protected static object _channelMutex = new object();

        protected ConcurrentDictionary<string, Type>
            _handlers = new ConcurrentDictionary<string, Type>();

        protected ConcurrentDictionary<string, string>
            _consumers = new ConcurrentDictionary<string, string>();

        public bool IsOpen
        {
            get
            {
                lock (_channelMutex)
                {
                    if (_channel != null)
                    {
                        return _channel.IsOpen;
                    }

                    return false;
                }
            }
        }

        public RabbitMQAdapter(string hostname)
        {
            _hostname = hostname;
        }

        public IBusAdapter Username(string username)
        {
            _username = username;

            return this;
        }

        public IBusAdapter Password(string password)
        {
            _password = password;

            return this;
        }

        public bool Publish<TEvent>(TEvent EventMessage) where TEvent : IEvent
        {
            var message = Convert.ChangeType(EventMessage, typeof(TEvent));
            var serialized = message.SerializeToXml();
            var channelQueue = message.GetType().FullName;

            DeliverMessage(channelQueue, serialized);

            return true;
        }

        public async Task<bool> PublishAsync<TEvent>(TEvent EventMessage) 
            where TEvent : IEvent
        {
            return await Task.Run(() => Publish(EventMessage));
        }

        public bool Send<TCommand>(TCommand CommandMessage) 
            where TCommand : ICommand
        {
            var message = Convert.ChangeType(CommandMessage, typeof(TCommand));
            var serialized = message.SerializeToXml();
            var channelQueue = message.GetType().FullName;

            DeliverMessage(channelQueue, serialized);

            return true;
        }

        public async Task<bool> SendAsync<TCommand>(TCommand CommandMessage) 
            where TCommand : ICommand
        {
            return await Task.Run(() => Send(CommandMessage));
        }

        public void AddHandlers(List<Type> handlers)
        {
            Condition.NotNull(handlers);
            Condition.NotEmpty(handlers);

            var builder = new ContainerBuilder();

            foreach (var handler in handlers)
            {
                var messageFQCN = handler.GetInterfaces()
                    .FirstOrDefault(x =>
                        x.IsGenericType
                        && x.GetGenericTypeDefinition() == typeof(IHandler<>))
                    .GetGenericArguments()[0].FullName; 
                
                var handlerFQCN = handler.FullName;
                _handlers[messageFQCN] = handler;

                builder.RegisterType(handler).Named(messageFQCN, handler);
            }

            _locator = builder.Build();
        }

        protected void DeliverMessage(
            string channelQueue, string serializedMessage)
        {
            lock (_channelMutex)
            {
                if (!IsOpen) { return; }

                _channel.QueueDeclare(queue: channelQueue,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(exchange: "",
                                      routingKey: channelQueue,
                                      basicProperties: properties,
                                      body: GetBytes(serializedMessage));
            }
        }

        protected byte[] GetBytes(string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        protected void RegisterHandlers()
        {
            foreach (var messages in _handlers)
            {
                _channel.QueueDeclare(queue: messages.Key,
                      durable: true,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                var consumer = new EventingBasicConsumer(_channel);

                // start consuming
                consumer.Received += (model, ea) =>
                {
                    var stringType = messages.Key;
                    var body = ea.Body;
                    var messageString = Encoding.UTF8.GetString(body);
                    var type = GetType(stringType);

                    using (TextReader reader = new StringReader(messageString))
                    {
                        try
                        {
                            var m = new XmlSerializer(type, new Type[] { type })
                                .Deserialize(reader);

                            Type handlerType;
                            if (!_handlers.TryGetValue(stringType, out handlerType))
                            {
                                return;
                            }

                            var handlerCtx = new BusContext();
                            var handler =  _locator.ResolveNamed(stringType, handlerType);
                            var result = (bool) handler
                                .GetType()
                                .GetMethod("Handle")
                                .Invoke(handler, new[] { handlerCtx, m });

                            if (result)
                            {
                                lock (_channelMutex)
                                {
                                    _channel.BasicAck(
                                                deliveryTag: ea.DeliveryTag,
                                                multiple: false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                "An error occurred Unserializing from XML.", ex);
                        }
                    }
                };


                var consumerKeyTag = _channel.BasicConsume(queue: messages.Key,
                                     noAck: false,
                                     consumer: consumer);

                _consumers.TryAdd(messages.Key, consumerKeyTag);
            }
        }

        protected Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        #region StartStop

        public void Start()
        {
            if (IsStarted) { return; }

            IsStarted = true;
            var factory = new ConnectionFactory() 
            { 
                HostName = _hostname,
                UserName = _username, 
                Password = _password 
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            RegisterHandlers();
        }

        public void Stop()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Bus not started.");
            }

            lock (_channelMutex)
            {
                _consumers
                    .ToList()
                    .ForEach(kv => _channel.BasicCancel(kv.Value));

                _channel.Close();
                _connection.Close();
            }

            IsStarted = false;
        }

        #endregion StartStop

        #region IDisposable Support

        private bool _disposedValue;
        protected bool IsDisposed
        {
            get { return _disposedValue; }
            set { _disposedValue = value; }
        }
                
        protected void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    lock (_channelMutex)
                    {
                        _channel.Close();
                        _channel.Dispose();
                        _connection.Close();
                        _connection.Dispose();

                        _channel = null;
                        _connection = null;
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}
