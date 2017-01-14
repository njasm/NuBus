using System;
using Autofac;

namespace NuBus
{
    public interface IBusConfigurator
    {
        IBus Build();
        IBusConfigurator WithContainer(IContainer container);
        IBusConfigurator AddEndpoint(IEndPointConfiguration endpoint);
    }
}
