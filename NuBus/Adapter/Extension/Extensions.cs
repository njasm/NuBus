using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace NuBus.Adapter.Extension
{
    public static class Extensions
    {
        public static IBusConfigurator UseRabbitMQ(
            this IBusConfigurator ctg, string host, Action<IBusConfigurator> func)
        {
            ctg.SetBusAdapter(new RabbitMQAdapter(host));
            func(ctg);

            return ctg;
        }

        public static void AsPointToPoint(
            this IBusConfigurator ctg, Action<IBusConfigurator> act)
        {
            act(ctg);
        }

        public static void RegisterAssemblyMessages(this IBusConfigurator ctg)
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
                    
                    ctg.AddEventMessage(t);
                }
                else if (baseCommandType.IsAssignableFrom(t))
                {
                    Trace.WriteLine(
                        string.Format("Registering Command {0}", t.FullName));
                    
                    ctg.AddCommandMessage(t);
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
                cfg.AddHandler(h);
            }
        }

        internal static string SerializeToXml<T>(this T value) where T : class
        {
            if (value == null)
            {
                return string.Empty;
            }

            try
            {
                var xmlserializer = new XmlSerializer(
                    value.GetType(), new Type[] { value.GetType() });
                
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter))
                {
                    xmlserializer.Serialize(writer, value);

                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "An error occurred Serializing to XML.", ex);
            }
        }
    }
}
