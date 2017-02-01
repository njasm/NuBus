using System;
using System.Collections;

namespace NuBus.Util
{
	public static class Condition
	{
        [Obsolete("Will be removed in future releases. Use NotNull() instead.")]
		public static void GuardAgainstNull<T>(T obj) where T : class
		{
            NotNull(obj);
		}

        public static void NotEmpty<T>(T obj) where T : ICollection
        {
            NotNull(obj);

            if (obj.Count == 0)
            {
                throw new ArgumentException("Enumerable is empty");
            }
        }

        public static bool IsTrue(bool value)
        {
            if (!value)
            {
                return false;
            }

            return true;
        }

        public static void IsTrue<T>(bool value, string exMessage) where T : Exception
        {
            if (!value)
            {
                throw (T) new Exception(exMessage);
            }    
        }

        public static void NotNull(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }
	}
}
