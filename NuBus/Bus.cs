using System;
using System.Threading.Tasks;
using Autofac;
using NuBus.Util;

namespace NuBus
{
	public sealed class Bus : IBus
	{
		IBusAdapter _busAdapter;
        IContainer _container;

        public Bus()
        {
        }

        public Bus(IBusAdapter adapter) 
            : this()
		{
			Condition.NotNull(adapter);
			_busAdapter = adapter;
		}

        internal void AddAdapter(IBusAdapter adapter)
        {
            Condition.NotNull(adapter);
            _busAdapter = adapter;
        }


		public void Start()
		{
            Condition.NotNull(_busAdapter);
			_busAdapter.Start();
		}

		public void Stop()
		{
			_busAdapter.Stop();
		}

		public async Task<bool> PublishAsync<T>(T EventMessage) 
            where T : IEvent
		{
            Condition.NotNull(Convert.ChangeType(EventMessage, typeof(T)));

			return await Task.Run(() => _busAdapter.PublishAsync(EventMessage));
		}

		public async Task<bool> SendAsync<T>(T CommandMessage) 
            where T : ICommand
		{
            Condition.NotNull(Convert.ChangeType(CommandMessage, typeof(T)));

			return await Task.Run(() => _busAdapter.SendAsync(CommandMessage));
		}

		public bool Publish<T>(T EventMessage) where T : IEvent
		{
            Condition.NotNull(Convert.ChangeType(EventMessage, typeof(T)));

			return _busAdapter.Publish(EventMessage);
		}

		public bool Send<T>(T CommandMessage) where T : ICommand
		{
            Condition.NotNull(Convert.ChangeType(CommandMessage, typeof(T)));

			return _busAdapter.Send(CommandMessage);
		}
	}
}
