using System;
using NuBus;

namespace NuBusTest.Message
{
	public abstract class BaseCommand : ICommand
	{
		public Guid ID { get; set; }

		public BaseCommand()
		{
			ID = Guid.NewGuid();
		}
	}
}
