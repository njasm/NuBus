using System;
using System.Collections.Generic;
using NuBus;
using NuBus.Adapter;

namespace NuBusTest.Implementation
{
	public class SpyBehaviorBusAdapter : IBusAdapter
	{
		public bool UsernameCalled { get; set; }
		public bool PasswordCalled { get; set; }
		public bool IsStartedCalled { get; set; }
		public bool IsPublishCalled { get; set; }
		public bool IsSendCalled { get; set; }
		public bool StartCalled { get; set; }
		public bool StopCalled { get; set; }
		public bool AcknowledgeMessageCalled { get; set; }
		public bool AddHandlersCalled { get; set; }


		public bool IsStarted
		{
			get
			{
				this.IsStartedCalled = true;
				return true;
			}
		}

		public event EventHandler<MessageReceivedArgs> HandleMessageReceived;

		public bool AcknowledgeMessage(Guid messageID)
		{
			this.AcknowledgeMessageCalled = true;
			return true;
		}

		public void AddHandlers(List<Type> handlers)
		{
			this.AddHandlersCalled = true;
		}

		public IBusAdapter Password(string password)
		{
			this.PasswordCalled = true;
			return this;
		}

		public bool Publish<TEvent>(TEvent EventMessage) where TEvent : IEvent
		{
			this.IsPublishCalled = true;
			return true;
		}

		public bool Send<TCommand>(TCommand CommandMessage) where TCommand : ICommand
		{
			this.IsSendCalled = true;
			return true;
		}

		public void Start()
		{
			this.StartCalled = true;
		}

		public void Stop()
		{
			this.StopCalled = true;
		}

		public IBusAdapter Username(string username)
		{
			this.UsernameCalled = true;
			return this;
		}

		public void FireMessageReceivedHandlers(MessageReceivedArgs e)
		{
			HandleMessageReceived(this, e);
		}
	}
}
