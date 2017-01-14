using System;
using NuBus;
using NuBus.Adapter.Extension;

namespace NuBusTest
{
	public class BaseTest
	{
		public IBus GetBasicBus(string host = "localhost", string user = "guest", string pass = "guest")
		{
			var ctg = new BusConfigurator();
			ctg.UseRabbitMQ(host,
				(IBusConfigurator obj, IEndPointConfiguration endpoint) =>
				{
					endpoint.Username(user);
					endpoint.Password(pass);

					endpoint.RegisterAssemblyMessages();
					endpoint.RegisterAssemblyHandlers();
				}
			);
			//ctg.WithContainer(container);

			return ctg.Build();
		}
	}
}
