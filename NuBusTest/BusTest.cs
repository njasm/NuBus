using NUnit.Framework;
using System;
namespace NuBusTest
{
	using System.Threading;
	using Autofac;
	using NuBus;
	using Helper;
	using Handler;
	using Message;

	[TestFixture]
	public class BusTest : BaseTest
	{
		public const int SLEEP_MILLISECONDS = 1000;

		[SetUp]
		public void StartByDrainingMessages()
		{
			var bus = GetBasicBus();
			bus.Start();
			Thread.Sleep(SLEEP_MILLISECONDS);
			bus.Stop();
		}

		[Test]
		public void TestArgumentNullExceptionOnBusCreation()
		{
			Assert.Throws(typeof(ArgumentNullException),() => new Bus(null)); 
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
			var builder = new ContainerBuilder();
			builder
				.RegisterType<MessageValueBag<Guid, CommandOneHandler>>()
				.AsSelf()
				.SingleInstance();

			var container = builder.Build();
			var bus = GetBasicBus("localhost", "guest", "guest", container);

			var message = new CommandOne();
			bus.Start();
			bus.Send(message);

			Thread.Sleep(SLEEP_MILLISECONDS);
			bus.Stop();

			var bag = container.Resolve<MessageValueBag<Guid, CommandOneHandler>>();
			Assert.AreEqual(bag.Value, message.ID);
		}

		[Test]
		public void TestSendAsyncCommand()
		{
			var bus = GetBasicBus();

			bus.Start();
			var task = bus.SendAsync(new Message.CommandTwo());

			Assert.True(task.Result);
			bus.Stop();
		}

		[Test]
		public void TestPublishEvent()
		{
			var bus = GetBasicBus();

			bus.Start();
			bus.Publish(new Message.EventOne());
			bus.Stop();
		}

		[Test]
		public void TestPublishAsyncEvent()
		{
			var bus = GetBasicBus();

			bus.Start();
			var task = bus.PublishAsync(new Message.EventTwo());

			Assert.True(task.Result);
			bus.Stop();
		}

		[TearDown]
		public void DrainMessages()
		{
			var bus = GetBasicBus();
			bus.Start();
			Thread.Sleep(SLEEP_MILLISECONDS);
			bus.Stop();
		}
	}
}
