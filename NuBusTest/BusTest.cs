using NUnit.Framework;
using System;
namespace NuBusTest
{
	using NuBus;

	[TestFixture]
	public class BusTest
	{
		[Test]
		public void TestArgumentNullExceptionOnBusCreation()
		{
			Assert.Throws(typeof(ArgumentNullException),() => new Bus(null)); 
		}
	}
}
