using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NuBus.Util;

namespace NuBus
{
    public class BusConfigurator : IBusConfigurator
    {
        protected IBusAdapter _adapter;
        protected string _username;
        protected string _password;

        protected ConcurrentBag<Type> 
            _messages = new ConcurrentBag<Type>();

        protected ConcurrentBag<Type>
            _handlers = new ConcurrentBag<Type>();

        public void AddEventMessage(Type t)
        {
            Condition.NotNull(t);

            if (!typeof(IEvent).IsAssignableFrom(t))
            {
                throw new InvalidOperationException("Event Wrong Type");
            }

            _messages.Add(t);
        }

        public void AddCommandMessage(Type t)
        {
            Condition.NotNull(t);

            if (!typeof(ICommand).IsAssignableFrom(t))
            {
                throw new InvalidOperationException("ICommand Wrong Type");
            }

            _messages.Add(t);
        }

        public void AddHandler(Type t)
        {
            Condition.NotNull(t);

            if (t.IsInterface || t.IsAbstract)
            {
                throw new InvalidOperationException("Handler isn't Instantiable");
            }

            _handlers.Add(t);
        }


        public IBusConfigurator SetBusAdapter(IBusAdapter adapter)
        {
            _adapter = adapter;

            return this;
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

            return new Bus(_adapter);
        }
    }
}
