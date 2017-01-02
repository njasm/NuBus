using System;
using NUnit.Framework;

namespace NuBusTest
{
	using System.Threading;
	using NuBus;
	using NuBus.Adapter.Extension;

	[TestFixture]
	public class BusConfiguratorTest
	{
		public IBus GetBasicBus()
		{
			var ctg = (new BusConfigurator()).UseRabbitMQ("localhost",
				(IBusConfigurator obj) =>
				{
					obj.Username("guest");
					obj.Password("guest");

					obj.RegisterAssemblyMessages();
					obj.RegisterAssemblyHandlers();
				}
			);

			return ctg.Build();
		}

		[Test]
		public void TestInstantiation()
		{
			var bus = GetBasicBus();
			Assert.IsInstanceOf(typeof(IBus), bus);
		}

		[Test]
		public void TestSendCommand()
		{
			var bus = GetBasicBus();

			bus.Start();
			bus.Send(new Message.PointToPointMessage());
			bus.Stop();
		}

		[Test]
		public void TestSendAsyncCommand()
		{
			var bus = GetBasicBus();

			bus.Start();
			var task = bus.SendAsync(new Message.PointToPointMessage());

			Assert.True(task.Result);
			bus.Stop();
		}

		[Test]
		public void TestPublishEvent()
		{
			var bus = GetBasicBus();

			bus.Start();
			bus.Publish(new Message.PointToPointMessage());
			bus.Stop();
		}

		[Test]
		public void TestPublishAsyncEvent()
		{
			var bus = GetBasicBus();

			bus.Start();
			var task = bus.PublishAsync(new Message.PointToPointMessage());

			Assert.True(task.Result);
			bus.Stop();
		}
	}
}
