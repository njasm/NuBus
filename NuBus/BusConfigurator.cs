using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using NuBus.Util;
using NuBus.Service;

namespace NuBus
{
    public sealed class BusConfigurator : IBusConfigurator
    {
        Bus _bus;
        IContainer _container;
        IBusAdapter _adapter;

        IEndpointService _service;
        IEndPointConfiguration _activeEndpoint;
        HashSet<IEndPointConfiguration> _endpoints = new HashSet<IEndPointConfiguration>();

        public BusConfigurator()
        {
            _service = new EndpointService();
            _bus = new Bus(_service);
        }

        public IBusConfigurator WithContainer(IContainer container)
        {
            _container = container;
            _bus.AddContainer(container);

            return this;
        }

        public IBusConfigurator AddEndpoint(IEndPointConfiguration endpoint)
        {
            Condition.NotNull(endpoint);

            _activeEndpoint = endpoint;
            _service.AddEndpoint(endpoint);

            return this;
        }

        public IBus Build()
        {
            var b = new ContainerBuilder();
            b.RegisterInstance(_bus).As<IBus>()
                .AsSelf().AsImplementedInterfaces();

            var messages = _service.GetAllMessages();
            RegisterContainerMessages(b, messages);

            var handlers = _service.GetAllHandlers();
            RegisterContainerHandlers(b, handlers);

            if (_container == null)
            {
                _container = b.Build();
            }
            else
            {
                b.Update(_container);
            }

            //_bus.AddMessages(_messages);
            //_bus.AddHandlers(_handlers);

            return _bus;
        }

        #region Private Methods

        private void RegisterContainerHandlers(
            ContainerBuilder builder, IReadOnlyCollection<Type> handlers)
        {
            Condition.NotNull(handlers);

            handlers
                .ToList()
                .ForEach(h => 
                    {
                        var messageFQCN = h.GetInterfaces()
                            .FirstOrDefault(x =>
                                x.IsGenericType
                                && x.GetGenericTypeDefinition() == typeof(IHandler<>))
                            .GetGenericArguments()[0].FullName;

                        builder.RegisterType(h).Named(h.FullName, h);
                    });
        }

        private void RegisterContainerMessages(
            ContainerBuilder builder, IReadOnlyCollection<Type> messages)
        {
            Condition.NotNull(messages);

            messages
                .ToList()
                .ForEach(
                    m => builder.RegisterType(m).Named(m.FullName, m));
        }

        #endregion Private Methods
    }
}
