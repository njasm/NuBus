using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Autofac;
using NuBus.Adapter;
using NuBus.Service;
using NuBus.Util;
using RabbitMQ.Client.Events;

namespace NuBus
{
	public sealed class Bus : IBus
	{
        IContainer _container;
        IEndpointService _service;

        public Bus(IEndpointService service)
        {
            Condition.NotNull(service);
            _service = service;
        }

        internal void AddAdapter(IBusAdapter adapter)
        {
            //_busAdapter.HandleMessageReceived += OnMessageReceived;
        }

        public void OnMessageReceived(object sender, MessageReceivedArgs e)
        {
            Type messageType = GetType(e.MessageKey);
            Type handlerType = _service
                .GetAllHandlers()
                .FirstOrDefault(h => h.GetGenericArguments()[0].FullName == messageType.FullName);
            if (handlerType == null)
            {
                throw new InvalidOperationException(string.Format(
                    "No Handler Registered for handling of {0}", e.MessageKey));
            }

            using (TextReader reader = new StringReader(e.SerializedMessage))
            {
                try
                {
                    var m = new XmlSerializer(messageType, new Type[] { messageType })
                        .Deserialize(reader);
                    
                    var handlerCtx = new BusContext();
                    var handler = _container.ResolveNamed(handlerType.FullName, handlerType);
                    var result = (bool)handler
                        .GetType()
                        .GetMethod("Handle")
                        .Invoke(handler, new[] { handlerCtx, m });

                    if (result)
                    {
                        //(sender as EventingBasicConsumer).Model.BasicAck(1, false);
                        //_busAdapter.AcknowledgeMessage(e.MessageID);
                    }

                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "An error occurred Unserializing from XML.", ex);
                }
            }
        }

        Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }


        internal void AddHandlers(ConcurrentBag<Type> handlers)
        {
            Condition.NotNull(handlers);
            Condition.NotEmpty(handlers);

            foreach (var handler in handlers)
            {
                var messageFQCN = handler.GetInterfaces()
                    .FirstOrDefault(x =>
                        x.IsGenericType
                        && x.GetGenericTypeDefinition() == typeof(IHandler<>))
                    .GetGenericArguments()[0].FullName;

                var handlerFQCN = handler.FullName;
                //_handlers[messageFQCN] = handler;
            }
        }

        public void AddContainer(IContainer container)
        {
            Condition.NotNull(container);
            _container = container;
        }

		public void Start()
		{
            Condition.NotNull(_service);
            _service.StartAll();
		}

		public void Stop()
		{
            _service.StopAll();
		}

		public async Task<bool> PublishAsync<T>(T EventMessage) 
            where T : IEvent
		{
            Condition.NotNull(Convert.ChangeType(EventMessage, typeof(T)));

			return await Task.Run(() => _service.Publish(EventMessage));
		}

		public async Task<bool> SendAsync<T>(T CommandMessage) 
            where T : ICommand
		{
            Condition.NotNull(Convert.ChangeType(CommandMessage, typeof(T)));

			return await Task.Run(() => _service.Send(CommandMessage));
		}

		public bool Publish<T>(T EventMessage) where T : IEvent
		{
            Condition.NotNull(Convert.ChangeType(EventMessage, typeof(T)));

			return _service.Publish(EventMessage);
		}

		public bool Send<T>(T CommandMessage) where T : ICommand
		{
            Condition.NotNull(Convert.ChangeType(CommandMessage, typeof(T)));

			return _service.Send(CommandMessage);
		}
	}
}
