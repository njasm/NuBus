using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NuBus.Adapter;
using NuBus.Util;

namespace NuBus.Service
{
    public class EndpointService : IEndpointService
    {
        ConcurrentBag<IEndPointConfiguration> _endpoints = 
            new ConcurrentBag<IEndPointConfiguration>();

        ConcurrentDictionary<Guid, Dictionary<DateTime, IEndPointConfiguration>> _deliveringMessages
            = new ConcurrentDictionary<Guid, Dictionary<DateTime, IEndPointConfiguration>>();

        public event EventHandler<MessageReceivedArgs> HandleMessageReceived;

        public void AddEndpoint(IEndPointConfiguration endpoint)
        {
            _endpoints.Add(endpoint);
            endpoint.HandleMessageReceived += OnMessageReceived;
        }

        public void StartAll()
        {
            _endpoints.ToList().ForEach(
                e => 
                    { 
                        var h = e.GetHandlers(); 
                        e.GetBusAdapter()
                            .AddHandlers(h.ToList()); 
                    });

            _endpoints
                .Select(e => e.GetBusAdapter())
                .ToList()
                .ForEach(a => a.Start());
        }

        public void StopAll()
        {
            _endpoints
                .Select(e => e.GetBusAdapter())
                .ToList()
                .ForEach(a => a.Stop());
        }

        public bool Publish<T>(T message) where T : IEvent
        {
            _endpoints
                .Where(e => e.GetMessages().Any(t => t.FullName == typeof(T).FullName))
                .Select(e => e.GetBusAdapter())
                .AsParallel()
                .ForAll(a => a.Publish(message));

            return true;
        }

        public bool Send<T>(T message) where T : ICommand
        {
            _endpoints
                .Where(e => e.GetMessages().Any(t => t.FullName == typeof(T).FullName))
                .Select(e => e.GetBusAdapter())
                .AsParallel()
                .ForAll(a => a.Send(message));

            return true;
        }

        public IReadOnlyCollection<Type> GetAllMessages()
        {
            var messages = new List<Type>();
            _endpoints.ToList().ForEach(e =>
            {
                e.GetMessages().ToList().ForEach(m => 
                {
                    if (!messages.Any(rm => rm.FullName == m.FullName))
                    {
                        messages.Add(m);
                    }
                });
            });

            return messages.AsReadOnly();
        }


        public IReadOnlyCollection<Type> GetAllHandlers()
        {
            var handlers = new List<Type>();
            _endpoints.ToList().ForEach(e =>
            {
                e.GetHandlers().ToList().ForEach(h =>
                {
                    if (!handlers.Any(rh => rh.FullName == h.FullName))
                    {
                        handlers.Add(h);
                    }
                });
            });

            return handlers.AsReadOnly();
        }

        public Type GetHandlerFor(string messageFQCN)
        {
            Condition.NotNull(messageFQCN);

            return GetAllHandlers()
                .FirstOrDefault(
                    h => h
                        .GetInterfaces()
                        .Any(x =>
                            x.IsGenericType
                            && x.GetGenericTypeDefinition() == typeof(IHandler<>)
                            && x.GetGenericArguments()[0].FullName == messageFQCN));
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnMessageReceived(object sender, MessageReceivedArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<MessageReceivedArgs> handler = HandleMessageReceived;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                _deliveringMessages.TryAdd(
                    e.MessageID, 
                    new Dictionary<DateTime, IEndPointConfiguration>()
                    {
                        { DateTime.Now, (IEndPointConfiguration)sender },
                    });
                                           
                handler(this, e);
            }
        }

        public void AcknowledgeMessage(Guid messageID)
        {
            if (_deliveringMessages.ContainsKey(messageID))
            {
                _deliveringMessages
                    .First(m => m.Key == messageID)
                    .Value.First()
                    .Value.GetBusAdapter().AcknowledgeMessage(messageID);

                Dictionary<DateTime, IEndPointConfiguration> removed;
                _deliveringMessages.TryRemove(messageID, out removed);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EndpointService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
