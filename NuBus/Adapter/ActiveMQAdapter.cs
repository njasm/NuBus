using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using NuBus.Util;
using NuBus.Extension;

namespace NuBus.Adapter
{
    public class ActiveMQAdapter : IBusAdapter, IDisposable
    {
        public event EventHandler<MessageReceivedArgs> HandleMessageReceived;

        public bool IsStarted
        {
            get
            {
                return (_session != null && _connection.IsStarted)
                    ? true : false;
            }
        }

        protected ConcurrentDictionary<Guid, Tuple<IMessageConsumer, Apache.NMS.IMessage>>
            _deliveringMessages = new ConcurrentDictionary<Guid, Tuple<IMessageConsumer, Apache.NMS.IMessage>>();

        protected ConcurrentDictionary<string, Type>
            _handlers = new ConcurrentDictionary<string, Type>();

        protected ConcurrentDictionary<string, IMessageConsumer>
            _consumers = new ConcurrentDictionary<string, IMessageConsumer>();

        protected string _username;
        protected string _password;
        protected string _hostname;

        protected IConnection _connection;
        protected ISession _session;

        public ActiveMQAdapter(string hostname)
        {
            Condition.GuardAgainstNull(hostname);
            _hostname = hostname;
        }


        public void Start()
        {
            if (IsStarted) { return; }

            var factory = new ConnectionFactory(_hostname);

            _connection = factory.CreateConnection(_username, _password);
            _connection.Start();
            _session = _connection.CreateSession(AcknowledgementMode.IndividualAcknowledge);

            RegisterHandlers();
        }

        void RegisterHandlers()
        {
            foreach (var handler in _handlers)
            {
                var destinationQueue = _session.GetQueue(handler.Key);
                var consumer = _session.CreateConsumer(destinationQueue);
                consumer.Listener += (Apache.NMS.IMessage message) =>
                {
                    var textMessage = (ITextMessage)message;
                    var mKey = handler.Key;
                    var serializer = textMessage.Text;
                    var id = Guid.NewGuid();
                    var args = new MessageReceivedArgs(mKey, serializer, id);

                    var t = new Tuple<IMessageConsumer, Apache.NMS.IMessage>(null, message);
                    _deliveringMessages.TryAdd(id, t);

                    OnMessageReceived(args);
                };

                _consumers.AddOrUpdate(handler.Key, key => consumer, (key, oldConsumer) => consumer);
            }
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

        public bool AcknowledgeMessage(Guid messageID)
        {
            Tuple<IMessageConsumer, Apache.NMS.IMessage> t;
            if (!_deliveringMessages.TryGetValue(messageID, out t))
            {
                return false;
            }

            t.Item2.Acknowledge();
            _deliveringMessages.TryRemove(messageID, out t);

            return true;
        }

        public void Stop()
        {
            _session.Close();
            _connection.Close();
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

        public bool Publish<TEvent>(TEvent EventMessage) where TEvent : IEvent
        {
            Condition.IsTrue<InvalidOperationException>(IsStarted, "Not Started!");

            var msg = Convert.ChangeType(EventMessage, typeof(TEvent));
            var messageSerialized = msg.SerializeToXml();

            DeliverMessage(msg.GetType().FullName, messageSerialized);

            return true;
        }

        public bool Send<TCommand>(TCommand CommandMessage) where TCommand : ICommand
        {
            Condition.IsTrue<InvalidOperationException>(IsStarted, "Not Started!");

            var msg = Convert.ChangeType(CommandMessage, typeof(TCommand));
            var messageSerialized = msg.SerializeToXml();

            DeliverMessage(msg.GetType().FullName, messageSerialized);

            return true;
        }

        protected void DeliverMessage(
            string queue, string serializedMessage)
        {
            // Create the destination (Topic or Queue)
            var destination = _session.GetQueue(queue);

            // Create a MessageProducer from the Session to the Topic or Queue
            var producer = _session.CreateProducer(destination);
            producer.DeliveryMode = MsgDeliveryMode.Persistent;

            // Create a messages
            var message = _session.CreateTextMessage(serializedMessage);

            // Tell the producer to send the message
            producer.Send(message);
            producer.Close();
            producer.Dispose();
        }

        #region IDisposable Support

        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _session.Dispose();
                    _session = null;

                    _connection.Dispose();
                    _connection = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
