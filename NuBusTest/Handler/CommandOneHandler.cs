using System;
using NuBus;
using NuBusTest.Message;

namespace NuBusTest.Handler
{
	public class CommandOneHandler : IHandler<CommandOne>
	{
		public bool Handle(IBusContext ctx, CommandOne Message)
		{
			return true;
		}
	}
}
