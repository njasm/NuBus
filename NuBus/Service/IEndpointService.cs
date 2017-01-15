using System;
using System.Collections.Generic;
using NuBus.Adapter;

namespace NuBus.Service
{
    public interface IEndpointService : IDisposable
    {
        event EventHandler<MessageReceivedArgs> HandleMessageReceived;

        void AddEndpoint(IEndPointConfiguration endpoint);

        bool Send<T>(T message) where T : ICommand;
        bool Publish<T>(T message) where T : IEvent;

        void StartAll();
        void StopAll();

        IReadOnlyCollection<Type> GetAllMessages();
        IReadOnlyCollection<Type> GetAllHandlers();

        Type GetHandlerFor(string messageFQCN);
        void AcknowledgeMessage(Guid messageID);
    }
}
