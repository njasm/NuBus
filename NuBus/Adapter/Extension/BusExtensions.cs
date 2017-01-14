using System;
using Autofac;

namespace NuBus.Adapter.Extension
{
    public static class BusExtensions
    {
        public static IBusConfigurator WithContainer(this IBusConfigurator cfg, IContainer container)
        {
            cfg.WithContainer(container);

            return cfg;
        }
    }
}
