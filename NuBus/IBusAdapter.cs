using System;
using System.Collections.Generic;
using NuBus.Adapter;
using RabbitMQ.Client;

namespace NuBus
{
	public interface IBusAdapter 
	{
        event EventHandler<MessageReceivedArgs> HandleMessageReceived;
        bool IsStarted { get; }

        void Start();
        void Stop();

        IBusAdapter Username(string username);
        IBusAdapter Password(string password);

        void AddHandlers(List<Type> handlers);
        bool AcknowledgeMessage(Guid messageID);

        bool Publish<TEvent>(TEvent EventMessage) where TEvent : IEvent;
        bool Send<TCommand>(TCommand CommandMessage) where TCommand : ICommand;
	}
}
