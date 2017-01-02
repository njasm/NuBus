using System;
using System.Text;
using System.Threading.Tasks;
using NuBus.Util;
using RabbitMQ.Client;

namespace NuBus.Adapter
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
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

        protected IConnection _connection;
        protected IModel _channel;
        protected object _channelMutex = new object();

        protected ConcurrentDictionary<string, Type>
            _handlers = new ConcurrentDictionary<string, Type>();

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
                var channel = _connection.CreateModel();
                channel.QueueDeclare(queue: messages.Key,
                      durable: true,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    // convert to object
                    // create IBusContext
                    // add this to IBusContext
                    // add ea as Envelope
                    // instantiate handler and call Handle
                    // ack if true.
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                // start consuming
                channel.BasicConsume(queue: messages.Key,
                                     noAck: false,
                                     consumer: consumer);
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
                _channel.Close();
            }

            _connection.Close();
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
