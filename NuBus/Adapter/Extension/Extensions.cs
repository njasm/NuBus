using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using NuBus.Util;

namespace NuBus.Adapter.Extension
{
    public static class Extensions
    {
        public static IBusConfigurator UseRabbitMQ(
           this IBusConfigurator cfg, string host, Action<IBusConfigurator> func)
        {
            (cfg as IBusConfiguratorInternal).SetBusAdapter(new RabbitMQAdapter(host));
            func(cfg);

            return cfg;
        }

        public static void AsPointToPoint(
            this IBusConfigurator ctg, Action<IBusConfigurator> act)
        {
            act(ctg);
        }

        public static void RegisterAssemblyMessages(this IBusConfigurator cfg)
        {
            var baseEventType = typeof(IEvent);
            var baseCommandType = typeof(ICommand);

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => (p.GetInterfaces().Contains(baseEventType)
                             || p.GetInterfaces().Contains(baseCommandType))
                       && !p.IsInterface && !p.IsAbstract)
                .ToList();

            foreach (var t in types)
            {
                if (baseEventType.IsAssignableFrom(t))
                {
                    Trace.WriteLine(
                        string.Format("Registering Event {0}", t.FullName));
                    
                    (cfg as IBusConfiguratorInternal).AddEventMessage(t);
                }
                else if (baseCommandType.IsAssignableFrom(t))
                {
                    Trace.WriteLine(
                        string.Format("Registering Command {0}", t.FullName));
                    
                    (cfg as IBusConfiguratorInternal).AddCommandMessage(t);
                }
            }
        }

        public static void RegisterAssemblyHandlers(this IBusConfigurator cfg)
        {
            var baseHandlerType = typeof(IHandler<>);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => 
                    !p.IsAbstract
                    && !p.IsInterface
                    && p.GetInterfaces()
                        .Any(x => 
                            x.IsGenericType
                             && x.GetGenericTypeDefinition() == baseHandlerType))
                .ToList();

            foreach (var h in types)
            {
                (cfg as IBusConfiguratorInternal).AddHandler(h);
            }
        }
    }
}
