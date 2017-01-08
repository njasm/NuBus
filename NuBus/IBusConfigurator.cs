using System;
using Autofac;

namespace NuBus
{
    public interface IBusConfigurator
    {
        IBusConfigurator Username(string username);
        IBusConfigurator Password(string Password);

        IBus Build();
    }

    interface IBusConfiguratorInternal : IBusConfigurator 
    {
        void AddEventMessage(Type t);
        void AddCommandMessage(Type t);

        void AddHandler(Type t);
        void WithContainer(IContainer container);

        void SetBusAdapter(IBusAdapter adapter);
    }
}
