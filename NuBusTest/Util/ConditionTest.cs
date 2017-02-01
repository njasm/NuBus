using NUnit.Framework;
using NuBus.Util;
using System;
using System.Collections.Generic;

namespace NuBusTest.Util
{
	[TestFixture]
	public class ConditionTest
	{
		[Test]
		public void TestIsTrue()
		{
			Assert.Multiple(() =>
			{
				bool val = false;
				Assert.IsFalse(Condition.IsTrue(val));

				val = true;
				Assert.IsTrue(Condition.IsTrue(val));
			});
		}

		[Test]
		public void TestIsTrueException()
		{
			Assert.Multiple(() =>
			{
			bool val = false;
			var exMessage = "My God!";

				Assert.Throws<Exception>(() => Condition.IsTrue<Exception>(val, exMessage), exMessage);
				Assert.Throws<InvalidCastException>(
					() => Condition.IsTrue<InvalidCastException>(val, exMessage), exMessage);

				val = true;
				Assert.DoesNotThrow(() => Condition.IsTrue<Exception>(val, exMessage), exMessage);
			});
		}

		[Test]
		public void TestNotEmpty()
		{
			Assert.Multiple(() =>
			{
				List<int> val = null;
				Assert.Throws<ArgumentNullException>(() => Condition.NotEmpty(val));

				val = new List<int>();
				Assert.Throws<ArgumentException>(() => Condition.NotEmpty(val));

				val.Add(1);
				Assert.DoesNotThrow(() => Condition.NotEmpty(val));
			});
		}
	}
}
