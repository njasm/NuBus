using System;
using NuBus;
using NuBus.Adapter.Extension;

namespace NuBusTest
{
	public class BaseTest
	{
		public IBus GetBasicBus(string host = "localhost", string user = "guest", string pass = "guest")
		{
			var ctg = (new BusConfigurator()).UseRabbitMQ(host,
				(IBusConfigurator obj) =>
				{
					obj.Username(user);
					obj.Password(pass);

					obj.RegisterAssemblyMessages();
					obj.RegisterAssemblyHandlers();

					//obj.WithContainer(container:);
				}
			);

			return ctg.Build();
		}
	}
}
