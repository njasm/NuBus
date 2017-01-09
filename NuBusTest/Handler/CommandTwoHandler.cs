using System;
using NuBus;
using NuBusTest.Message;

namespace NuBusTest.Handler
{
	public class CommandTwoHandler : IHandler<CommandTwo>
	{
		public bool Handle(IBusContext ctx, CommandTwo Message)
		{
			return true;
		}
	}
}
