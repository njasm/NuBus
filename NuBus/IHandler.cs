using System;
namespace NuBus
{
	public interface IHandler<T> where T : IEvent, ICommand
	{
		bool Handle(IBusContext ctx, T Message);
	}
}
