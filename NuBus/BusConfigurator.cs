using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using NuBus.Util;

namespace NuBus
{
    public sealed class BusConfigurator : IBusConfiguratorInternal, IBusConfigurator
    {
        Bus _bus;
        IContainer _container;
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

        void WithContainer(IContainer container)
        {
            _container = container;
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

            var b = new ContainerBuilder();
            b.RegisterInstance(_bus).As<IBus>()
                .AsSelf().AsImplementedInterfaces();

            RegisterContainerMessages(b, _messages);
            RegisterContainerHandlers(b, _handlers);

            if (_container == null)
            {
                _container = b.Build();
            }
            else
            {
                b.Update(_container);
            }

            _bus.AddContainer(_container);
            _bus.AddMessages(_messages);
            _bus.AddHandlers(_handlers);

            return _bus;
        }

        private void RegisterContainerHandlers(ContainerBuilder builder, ConcurrentBag<Type> handlers)
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

                builder.RegisterType(handler).Named(handler.FullName, handler);
            }
        }

        private void RegisterContainerMessages(
            ContainerBuilder builder, ConcurrentDictionary<MessageType, ConcurrentBag<Type>> messages)
        {
            Condition.NotNull(messages);
            Condition.NotEmpty(messages);

            foreach (var message in messages)
            {
                foreach (var t in message.Value)
                { 
                    builder.RegisterType(t).Named(t.FullName, t);                    
                }
            }
        }

        void IBusConfiguratorInternal.WithContainer(IContainer container)
        {
            _container = container;
        }
    }
}
