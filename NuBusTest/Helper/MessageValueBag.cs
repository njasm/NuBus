using System;
namespace NuBusTest.Helper
{
	public class MessageValueBag<TValue, THandler>
	{
		public TValue Value { get; set; }
		public THandler Handler { get; set; }
	}
}
