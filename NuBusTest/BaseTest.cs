using Autofac;
using NuBus;
using NuBus.Extension;

namespace NuBusTest
{
	public class BaseTest
	{
		public IBus GetBasicActiveMQBus(
			string host = "tcp://localhost:61616", string user = "guest", string pass = "guest", IContainer container = null)
		{
			var ctg = new BusConfigurator();
			ctg.UseActiveMQ(host,
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
