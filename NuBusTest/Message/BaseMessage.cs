using System;
using NuBus;

namespace NuBusTest.Message
{
	public abstract class BaseMessage : IEvent, ICommand
	{
		public Guid ID { get; set; }

		public BaseMessage()
		{
			ID = Guid.NewGuid();
		}
	}
}
