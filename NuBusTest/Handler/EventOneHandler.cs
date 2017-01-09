using System;
using NuBus;
using NuBusTest.Message;

namespace NuBusTest.Handler
{
	public class EventOneHandler : IHandler<EventOne>
	{
		IBus _bus;
		public EventOneHandler(IBus bus)
		{
			_bus = bus;
		}

		public bool Handle(IBusContext ctx, EventOne Message)
		{
			return true;
		}
	}
}
