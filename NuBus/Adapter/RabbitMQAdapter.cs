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

        public event EventHandler<MessageReceivedArgs> HandleMessageReceived;

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

        protected ConcurrentDictionary<Guid, Tuple<EventingBasicConsumer, BasicDeliverEventArgs>>
            _deliveringMessages = new ConcurrentDictionary<Guid, Tuple<EventingBasicConsumer, BasicDeliverEventArgs>>();

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
                consumer.Received += (model, ea) =>
                {
                    var stringType = messages.Key;
                    var body = ea.Body;
                    var messageString = Encoding.UTF8.GetString(body);
                    var args = new MessageReceivedArgs(messageKey: stringType,
                                                       serializedMessage: messageString,
                                                       messageID: Guid.NewGuid());

                    var t = new Tuple<EventingBasicConsumer, BasicDeliverEventArgs>((EventingBasicConsumer)model, ea);
                    _deliveringMessages.TryAdd(args.MessageID, t);

                    OnMessageReceived(args);
                };


                var consumerKeyTag = _channel.BasicConsume(queue: messages.Key,
                                     noAck: false,
                                     consumer: consumer);

                _consumers.TryAdd(messages.Key, consumerKeyTag);
            }
        }

        public void AddHandlers(List<Type> handlers)
        {
            Condition.NotNull(handlers);
            Condition.NotEmpty(handlers);

            foreach (var handler in handlers)
            {
                var messageFQCN = handler.GetInterfaces()
                    .FirstOrDefault(x =>
                        x.IsGenericType
                        && x.GetGenericTypeDefinition() == typeof(IHandler<>))
                    .GetGenericArguments()[0].FullName;

                var handlerFQCN = handler.FullName;
                _handlers[messageFQCN] = handler;
            }
        }
        
        public bool AcknowledgeMessage(Guid messageID)
        {
            Tuple<EventingBasicConsumer, BasicDeliverEventArgs> t;
            if (!_deliveringMessages.TryGetValue(messageID, out t))
            {
                return false;
            }

            lock (_channelMutex)
            {
                t.Item1.Model.BasicAck(
                    deliveryTag: t.Item2.DeliveryTag,
                    multiple: false);
            }

            _deliveringMessages.TryRemove(messageID, out t);

            return true;
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnMessageReceived(MessageReceivedArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MessageReceivedArgs> handler = HandleMessageReceived;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                handler(this, e);
            }
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
