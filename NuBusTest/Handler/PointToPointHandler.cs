using System;
using NuBus;
using NuBusTest.Message;

namespace NuBusTest.Handler
{
	public class PointToPointHandler : IHandler<PointToPointMessage>
	{

		public bool Handle(IBusContext ctx, PointToPointMessage Message)
		{
			
			return true;
		}
	}
}
