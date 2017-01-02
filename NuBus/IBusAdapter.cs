using System;
using System.Collections.Generic;

namespace NuBus
{
	public interface IBusAdapter : IBus
	{
        bool IsStarted { get; }

        IBusAdapter Username(string username);
        IBusAdapter Password(string password);

        void AddHandlers(List<Type> handlers);
	}
}
