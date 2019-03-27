using System;

namespace Kontur.Recognition.Utils
{
	public static class EnumUtils
	{
		public static TEnumType GetEnumValue<TEnumType>(int value) where TEnumType : struct, IConvertible
		{
			if (Enum.IsDefined(typeof(TEnumType), value))
			{
				return (TEnumType)Enum.ToObject(typeof(TEnumType), value);
			}
			throw new ArgumentException(string.Format("Given value {0} is not contained in enumeration {1}", value, typeof(TEnumType).FullName));
		}

		public static bool TryGetEnumValue<TEnumType>(int value, out TEnumType result) where TEnumType : struct, IConvertible
		{
			if (Enum.IsDefined(typeof(TEnumType), value))
			{
				result = (TEnumType)Enum.ToObject(typeof(TEnumType), value);
				return true;
			}
			result = default(TEnumType);
			return false;
		}
	}
}