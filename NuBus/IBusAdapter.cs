using System;
using System.Collections.Generic;
using NuBus.Adapter;
using RabbitMQ.Client;

namespace NuBus
{
	public interface IBusAdapter : IBus
	{
        event EventHandler<MessageReceivedArgs> HandleMessageReceived;
        bool IsStarted { get; }

        IBusAdapter Username(string username);
        IBusAdapter Password(string password);

        void AddHandlers(List<Type> handlers);
        bool AcknowledgeMessage(Guid messageID);
	}
}
