using NUnit.Framework;
using NuBus;
using NuBusTest.Implementation;
using System;
using NuBus.Adapter;
using NuBusTest.Message;
using NuBus.Service;

namespace NuBusTest
{
	[TestFixture]
	public class EndpointConfigurationTest
	{
		public string HostName = "localhost";
		public SpyBehaviorBusAdapter SpyAdapter { get; set; }

		[SetUp]
		public void SetUp()
		{
			SpyAdapter = new SpyBehaviorBusAdapter();
		}

		[TearDown]
		public void TearDown()
		{
			SpyAdapter = null;
		}

		[Test]
		public void TestThrowArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new EndPointConfiguration(null, SpyAdapter));
			Assert.Throws<ArgumentNullException>(() => new EndPointConfiguration(HostName, null));
		}

		[Test]
		public void TestNotThrow()
		{
			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);
			Assert.IsInstanceOf<IEndPointConfiguration>(endpoint);
		}

		[TestCase("john")]
		public void TestUsername(string username)
		{
			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);
			endpoint.Username(username);

			Assert.AreEqual(username, endpoint.GetUsername());
			Assert.True(SpyAdapter.UsernameCalled);
		}

		[TestCase("doe")]
		public void TestPassword(string password)
		{
			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);
			endpoint.Password(password);

			Assert.AreEqual(password, endpoint.GetPassword());
			Assert.True(SpyAdapter.PasswordCalled);
		}

		[TestCase("NotLocalHost")]
		public void TestHostnameIsSet(string host)
		{
			var endpoint = new EndPointConfiguration(host, SpyAdapter);

			Assert.AreEqual(host, endpoint.GetHostname());
		}

		[Test]
		public void TestAddHandler()
		{
			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);

			Assert.Multiple(() => 
			{
				Assert.DoesNotThrow(() => endpoint.AddHandler(typeof(Handler.CommandOneHandler)));

				Assert.Throws<ArgumentNullException>(() => endpoint.AddHandler(null));
				Assert.Throws<InvalidOperationException>(() => endpoint.AddHandler(typeof(IHandler<>)));
				Assert.Throws<InvalidOperationException>(() => endpoint.AddHandler(this.GetType()));
			});
		}

		[Test]
		public void TestMessageReceivedEventAggregateException()
		{
			EventHandler<MessageReceivedArgs> handler = 
				(sender, e) => { throw new Exception("dummy"); };

			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);
			endpoint.HandleMessageReceived += handler;

			Assert.Throws<AggregateException>(() => SpyAdapter.FireMessageReceivedHandlers(null));
		}

		[Test]
		public void TestMessageReceivedNotThrowsNoHandlers()
		{
			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);
			var eventArgs = new MessageReceivedArgs(
				typeof(CommandOne).FullName, string.Empty, Guid.NewGuid());

			Assert.DoesNotThrow(() => SpyAdapter.FireMessageReceivedHandlers(eventArgs));
			Assert.IsInstanceOf<EndPointConfiguration>(endpoint);
		}

		[Test]
		public void TestMessageReceivedNotThrows()
		{
			bool Called = false;
			EventHandler<MessageReceivedArgs> handler = (sender, e) => { Called = true; };

			var endpoint = new EndPointConfiguration(HostName, SpyAdapter);
			endpoint.HandleMessageReceived += handler;

			var eventArgs = new MessageReceivedArgs(
				typeof(CommandOne).FullName, string.Empty, Guid.NewGuid());

			Assert.DoesNotThrow(() => SpyAdapter.FireMessageReceivedHandlers(eventArgs));
			Assert.IsTrue(Called);
		}
	}
}
