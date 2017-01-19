using System;
using NuBus;
using NuBusTest.Message;
using Autofac;
using NuBusTest.Helper;

namespace NuBusTest.Handler
{
	public class CommandOneHandler : IHandler<CommandOne>
	{
		ILifetimeScope _container;
		public CommandOneHandler(ILifetimeScope container)
		{
			_container = container;
		}

		public bool Handle(IBusContext ctx, CommandOne Message)
		{
			var messageValueBag = _container
				.Resolve<MessageValueBag<Guid, CommandOneHandler>>();
			
			messageValueBag.Value = Message.ID;
			messageValueBag.Handler = this;

			return true;
		}
	}
}
