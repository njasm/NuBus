using System;
using NuBus;
using NuBusTest.Message;

namespace NuBusTest.Handler
{
	public class PointToPointHandler : IHandler<PointToPointMessage>
	{
		IBus _bus;
		public PointToPointHandler(IBus bus)
		{
			_bus = bus;
		}

		public bool Handle(IBusContext ctx, PointToPointMessage Message)
		{
			return true;
		}
	}
}
