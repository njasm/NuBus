using NUnit.Framework;
using System;
namespace NuBusTest
{
	using System.Threading;
	using NuBus;

	[TestFixture]
	public class BusTest : BaseTest
	{
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
			var bus = GetBasicBus();

			bus.Start();
			bus.Send(new Message.CommandOne());
			bus.Stop();
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
			Thread.Sleep(3000);
			bus.Stop();
		}
	}
}
