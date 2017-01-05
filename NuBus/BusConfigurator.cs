using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NuBus.Util;

namespace NuBus
{
    public sealed class BusConfigurator : IBusConfiguratorInternal, IBusConfigurator
    {
        Bus _bus;
        IBusAdapter _adapter;
        string _username;
        string _password;

        ConcurrentDictionary<MessageType, ConcurrentBag<Type>>
            _messages = new ConcurrentDictionary<MessageType, ConcurrentBag<Type>>();

        ConcurrentBag<Type> _handlers = new ConcurrentBag<Type>();

        public BusConfigurator()
        {
            _bus = new Bus();
        }

        void IBusConfiguratorInternal.AddEventMessage(Type t)
        {
            Condition.NotNull(t);

            if (!typeof(IEvent).IsAssignableFrom(t))
            {
                throw new InvalidOperationException("Event Wrong Type");
            }

            AddMessage(t, MessageType.Event);
        }

        void IBusConfiguratorInternal.AddCommandMessage(Type t)
        {
            Condition.NotNull(t);

            if (!typeof(ICommand).IsAssignableFrom(t))
            {
                throw new InvalidOperationException("ICommand Wrong Type");
            }

            AddMessage(t, MessageType.Command);
        }

        internal void AddMessage(Type t, MessageType mType = MessageType.Generic)
        { 
            if (!_messages.ContainsKey(mType))
            {
                _messages[mType] = new ConcurrentBag<Type>() { t };

                return;
            }

            ConcurrentBag<Type> s;
            _messages.TryGetValue(mType, out s);
            s.Add(t);
        }

        void IBusConfiguratorInternal.AddHandler(Type t)
        {
            Condition.NotNull(t);

            if (t.IsInterface || t.IsAbstract)
            {
                throw new InvalidOperationException("Handler isn't Instantiable");
            }

            _handlers.Add(t);
        }


        void IBusConfiguratorInternal.SetBusAdapter(IBusAdapter adapter)
        {
            _adapter = adapter;
            _bus.AddAdapter(_adapter);
        }

        public IBusConfigurator Username(string username)
        {
            _username = username;

            return this;
        }

        public IBusConfigurator Password(string password)
        {
            _password = password;

            return this;
        }

        public IBus Build()
        {
            _adapter
                .Username(_username)
                .Password(_password);

            _adapter.AddHandlers(new List<Type>(_handlers.ToArray()));

            return _bus;
        }
    }
}
