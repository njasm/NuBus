using System;
using NuBus;
using NuBusTest.Message;

namespace NuBusTest.Handler
{
	public class EventTwoHandler : IHandler<EventTwo>
	{
		public bool Handle(IBusContext ctx, EventTwo Message)
		{
			return true;
		}
	}
}
