using System;
using NuBusTest.Message;
using NUnit.Framework;
using NuBus.Extension;

namespace NuBusTest.Extension
{
	[TestFixture]
	public class XMLTest
	{
		[Test]
		public void TestSerializeAndUnserialize()
		{
			var subject = new CommandOne();
			var identifier = subject.ID;

			CommandTwo nullSubject = null;

			Assert.Multiple(() => 
			{
				string result = null;
				Assert.DoesNotThrow(() => result = nullSubject.SerializeToXml());
				Assert.IsNotNull(result);
				Assert.IsEmpty(result);

				Assert.DoesNotThrow(() => result = subject.SerializeToXml());
				Assert.IsNotEmpty(result);

				var resultObj = (CommandOne) result.UnserializeFromXml(subject.GetType().FullName);
				Assert.IsTrue(identifier.Equals(resultObj.ID));

				result += "malformed</>";
				Assert.Throws<InvalidOperationException>(() => result.UnserializeFromXml(subject.GetType().FullName));
			});
		}
	}
}
