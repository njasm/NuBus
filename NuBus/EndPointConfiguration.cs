using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuBus.Adapter;
using NuBus.Util;

namespace NuBus
{
    public class EndPointConfiguration : IEndPointConfiguration
    {
        protected string _hostname;
        protected string _username;
        protected string _password;

        IBusAdapter _adapter;

        protected ConcurrentDictionary<MessageType, ConcurrentBag<Type>>
            _messages = new ConcurrentDictionary<MessageType, ConcurrentBag<Type>>();

        protected ConcurrentBag<Type> _handlers = new ConcurrentBag<Type>();

        public event EventHandler<MessageReceivedArgs> HandleMessageReceived;

        public EndPointConfiguration(string hostname, IBusAdapter adapter)
        {
            Condition.NotNull(hostname);
            Condition.NotNull(adapter);

            _hostname = hostname;
            _adapter = adapter;
            _adapter.HandleMessageReceived += OnMessageReceived;
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnMessageReceived(object sender, MessageReceivedArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MessageReceivedArgs> handler;
            lock(this) 
            {
                handler = HandleMessageReceived;
            }

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                var exList = new List<Exception>();
                handler.GetInvocationList()
                    .ToList()
                    .ForEach(h => 
                    {
                        try
                        {
                            (h as EventHandler<MessageReceivedArgs>)(this, e);
                        }
                        catch (Exception internalEx)
                        {
                           exList.Add(internalEx);
                        }
                    });

                if (exList.Count > 0)
                {
                    throw new AggregateException(exList);
                }
            }
        }


        public string GetPassword()
        {
            return _password;
        }

        public string GetUsername()
        {
            return _username;
        }

        public string GetHostname()
        {
            return _hostname;
        }

        public void Username(string username)
        {
            _username = username;
            _adapter.Username(username);
        }

        public void Password(string password)
        {
            _password = password;
            _adapter.Password(password);
        }

        public void AddHandler(Type handler)
        {
            Condition.NotNull(handler);

            if (handler.IsInterface || handler.IsAbstract)
            {
                throw new InvalidOperationException("Handler isn't Instantiable");
            }

            bool isMessageHandler = handler.GetInterfaces()
                .Any(x => 
                     x.IsGenericType
                     && x.GetGenericTypeDefinition() == typeof(IHandler<>));

            if (!isMessageHandler)
            {
                throw new InvalidOperationException("Handler does not implement IHandler<T>");
            }

            _handlers.Add(handler);
        }

        public void AddMessage(Type Message)
        {
            var baseEventType = typeof(IEvent);
            var baseCommandType = typeof(ICommand);

            if (baseEventType.IsAssignableFrom(Message))
            {
                Trace.WriteLine(
                    string.Format("Registering Event {0}", Message.FullName));

                AddMessage(Message, MessageType.Event);
            }
            else if (baseCommandType.IsAssignableFrom(Message))
            {
                Trace.WriteLine(
                    string.Format("Registering Command {0}", Message.FullName));

                AddMessage(Message, MessageType.Command);
            }
        }

        protected void AddMessage(Type Message, MessageType mType)
        {
            if (!_messages.ContainsKey(mType))
            {
                _messages[mType] = new ConcurrentBag<Type>() { Message };

                return;
            }

            ConcurrentBag<Type> s;
            _messages.TryGetValue(mType, out s);
            s.Add(Message);
        }

        public void ClearHandlers()
        {
            throw new NotImplementedException();
        }

        public void ClearMessages()
        {
            throw new NotImplementedException();
        }

        public Type GetHandler(Type messageHandled)
        {
            throw new NotImplementedException();
        }

        public Type GetHandler(string messageHandledFQCN)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<Type> GetHandlers()
        {
            return _handlers.Distinct()
                .ToList().AsReadOnly();
        }

        public IReadOnlyCollection<Type> GetMessages()
        {
            return _messages
                .ToList()
                .SelectMany(t => t.Value.Select(m => m))
                .ToList()
                .Distinct()
                .ToList().AsReadOnly();
        }

        public IBusAdapter GetBusAdapter()
        {
            return _adapter;
        }
    }
}
