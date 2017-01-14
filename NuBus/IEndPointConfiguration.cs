using System;
using System.Collections.Generic;

namespace NuBus
{
    public interface IEndPointConfiguration
    {
        void Username(string username);
        void Password(string password);

        string GetUsername();
        string GetPassword();
        string GetHostname();

        void AddHandler(Type handler);
        Type GetHandler(string messageHandledFQCN);
        Type GetHandler(Type messageHandled);
        IReadOnlyCollection<Type> GetHandlers();
        void ClearHandlers();

        void AddMessage(Type Message);
        IReadOnlyCollection<Type> GetMessages();
        void ClearMessages();

        IBusAdapter GetBusAdapter();
    }
}
