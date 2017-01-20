using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using NuBus.Util;
using NuBus.Adapter.Extension;

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

        //protected ConcurrentDictionary<Guid, Tuple<EventingBasicConsumer, BasicDeliverEventArgs>>
        //    _deliveringMessages = new ConcurrentDictionary<Guid, Tuple<EventingBasicConsumer, BasicDeliverEventArgs>>();

        protected ConcurrentDictionary<string, Type>
            _handlers = new ConcurrentDictionary<string, Type>();

        protected ConcurrentDictionary<string, string>
            _consumers = new ConcurrentDictionary<string, string>();

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

        public bool AcknowledgeMessage(Guid messageID)
        {
            //throw new NotImplementedException();
            return true;
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
