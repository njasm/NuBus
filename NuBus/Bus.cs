using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Autofac;
using NuBus.Adapter;
using NuBus.Util;
using RabbitMQ.Client.Events;

namespace NuBus
{
	public sealed class Bus : IBus
	{
		IBusAdapter _busAdapter;
        IContainer _container;

        ConcurrentDictionary<MessageType, ConcurrentBag<Type>>
            _messages = new ConcurrentDictionary<MessageType, ConcurrentBag<Type>>();

        ConcurrentDictionary<string, Type>
            _handlers = new ConcurrentDictionary<string, Type>();

        public Bus()
        {
        }

        public Bus(IBusAdapter adapter) 
            : this()
		{
			Condition.NotNull(adapter);
			_busAdapter = adapter;
		}

        internal void AddAdapter(IBusAdapter adapter)
        {
            Condition.NotNull(adapter);
            _busAdapter = adapter;
            _busAdapter.HandleMessageReceived += OnMessageReceived;
        }

        public void OnMessageReceived(object sender, MessageReceivedArgs e)
        {
            Type messageType = GetType(e.MessageKey);
            Type handlerType;
            if (!_handlers.TryGetValue(e.MessageKey, out handlerType))
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
                        _busAdapter.AcknowledgeMessage(e.MessageID);
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

        internal void AddMessages(
            ConcurrentDictionary<MessageType, ConcurrentBag<Type>> messages)
        {
            Condition.NotNull(messages);
            _messages = messages;
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
                _handlers[messageFQCN] = handler;
            }
        }

        public void AddContainer(IContainer container)
        {
            Condition.NotNull(container);
            _container = container;
        }

		public void Start()
		{
            Condition.NotNull(_busAdapter);
			_busAdapter.Start();
		}

		public void Stop()
		{
			_busAdapter.Stop();
		}

		public async Task<bool> PublishAsync<T>(T EventMessage) 
            where T : IEvent
		{
            Condition.NotNull(Convert.ChangeType(EventMessage, typeof(T)));

			return await Task.Run(() => _busAdapter.PublishAsync(EventMessage));
		}

		public async Task<bool> SendAsync<T>(T CommandMessage) 
            where T : ICommand
		{
            Condition.NotNull(Convert.ChangeType(CommandMessage, typeof(T)));

			return await Task.Run(() => _busAdapter.SendAsync(CommandMessage));
		}

		public bool Publish<T>(T EventMessage) where T : IEvent
		{
            Condition.NotNull(Convert.ChangeType(EventMessage, typeof(T)));

			return _busAdapter.Publish(EventMessage);
		}

		public bool Send<T>(T CommandMessage) where T : ICommand
		{
            Condition.NotNull(Convert.ChangeType(CommandMessage, typeof(T)));

			return _busAdapter.Send(CommandMessage);
		}
	}
}
