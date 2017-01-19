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
            _service.HandleMessageReceived += OnMessageReceived;
        }

        public void OnMessageReceived(object sender, MessageReceivedArgs e)
        {
            Type messageType = GetType(e.MessageKey);
            Type handlerType = _service.GetHandlerFor(e.MessageKey);
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
                        _service.AcknowledgeMessage(e.MessageID);
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

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _service.Dispose();
                }

                _disposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
