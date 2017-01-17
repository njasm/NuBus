using NUnit.Framework;

namespace NuBusTest
{
	using NuBus.Service;

	[TestFixture]
	public class EndPointServiceTest
	{ 
		[Test]
		public void TestCallToDisposeByUsing()
		{
			using (var service = new EndpointService())
			{
			}
		}

	}
}