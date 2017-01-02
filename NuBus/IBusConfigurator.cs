using System;
namespace NuBus
{
    public interface IBusConfigurator
    {
        IBusConfigurator SetBusAdapter(IBusAdapter adapter);
        IBusConfigurator Username(string username);
        IBusConfigurator Password(string Password);

        IBus Build();

        void AddEventMessage(Type t);
        void AddCommandMessage(Type t);

        void AddHandler(Type t);
    }
}
