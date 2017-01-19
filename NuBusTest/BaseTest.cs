using System;
using Autofac;
using NuBus;
using NuBus.Adapter.Extension;

namespace NuBusTest
{
	public class BaseTest
	{
		public IBus GetBasicBus(
			string host = "localhost", string user = "guest", string pass = "guest", IContainer container = null)
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

			if (container != null) 
			{
				ctg.WithContainer(container);
			}

			return ctg.Build();
		}
	}
}
