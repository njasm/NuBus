using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NuBus.Service
{
    public class EndpointService : IEndpointService
    {
        ConcurrentBag<IEndPointConfiguration> _endpoints = 
            new ConcurrentBag<IEndPointConfiguration>();

        public void AddEndpoint(IEndPointConfiguration endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public void StartAll()
        {
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
                .ToList()
                .ForEach(a => a.Publish(message));

            return true;
        }

        public bool Send<T>(T message) where T : ICommand
        {
            _endpoints
                .Where(e => e.GetMessages().Any(t => t.FullName == typeof(T).FullName))
                .Select(e => e.GetBusAdapter())
                .ToList()
                .ForEach(a => a.Send(message));

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
