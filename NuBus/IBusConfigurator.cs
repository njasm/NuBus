using System;
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

        void SetBusAdapter(IBusAdapter adapter);
    }
}
