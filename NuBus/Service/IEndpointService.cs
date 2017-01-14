using System;
using System.Collections.Generic;

namespace NuBus.Service
{
    public interface IEndpointService : IDisposable
    {
        void AddEndpoint(IEndPointConfiguration endpoint);

        bool Send<T>(T message) where T : ICommand;
        bool Publish<T>(T message) where T : IEvent;

        void StartAll();
        void StopAll();

        IReadOnlyCollection<Type> GetAllMessages();
        IReadOnlyCollection<Type> GetAllHandlers();
    }
}
