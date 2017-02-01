using System;
using NuBus.Adapter;

namespace NuBus.Extension
{
    public static class Extensions
    {
        public static IBusConfigurator UseActiveMQ(
            this IBusConfigurator cfg, string host, Action<IBusConfigurator, IEndPointConfiguration> func)
        {
            var endpoint = new EndPointConfiguration(host, new ActiveMQAdapter(host));
            cfg.AddEndpoint(endpoint);
            func(cfg, endpoint);

            return cfg;
        }
    }
}
