using System;
using NuBus;

namespace NuBusTest.Message
{
	public abstract class BaseEvent : IEvent
	{
		public Guid ID { get; set; }

		public BaseEvent()
		{
			ID = Guid.NewGuid();
		}
	}
}
