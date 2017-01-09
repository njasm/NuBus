using System;
namespace NuBus
{
	public interface IHandler<T> where T : IMessage
	{
		bool Handle(IBusContext ctx, T Message);
	}
}
