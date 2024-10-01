namespace Chameleon.lib.Common.Extensions;
public static class ObjExts {
	public static object ParseValue(this object value)
	{
		// Try parsing value as int
		if (int.TryParse(value.ToString(), out var intValue))
			return intValue;

		// Try parsing value as bool
		if (bool.TryParse(value.ToString(), out var boolValue))
			return boolValue.ToString().ToLower();

		// Otherwise, treat it as a string
		return $"\"{value}\"";
	}
}

