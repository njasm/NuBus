using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using NuBus;
using NuBus.Util;

namespace NuBus.Adapter.Extension
{
    public static class Extensions
    {
        public static IBusConfigurator UseRabbitMQ(
           this IBusConfigurator cfg, string host, Action<IBusConfigurator, IEndPointConfiguration> func)
        {
            var endpoint = new EndPointConfiguration(host, new RabbitMQAdapter(host));
            cfg.AddEndpoint(endpoint);
            func(cfg, endpoint);

            return cfg;
        }

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
