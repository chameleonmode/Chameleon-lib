using System.ComponentModel;

namespace Chameleon.lib.Common.Constants;
public static class Enums {
	public enum ExtensionType {
		[Description("cromeleon")]
		chromeleon,
		[Description("chromeleon_auto_proxy")]
		chromeleon_auto_proxy,
		[Description("foxameleon")]
		foxameleon,
		[Description("foxameleon_proxy")]
		foxameleon_proxy,
	}

	public enum SystemBrowserType {
		Unknown,
		[Description("chrome")]
		Chrome,
		[Description("firefox")]
		Firefox,
		[Description("brave")]
		Brave,
		Chromium
	}

	public enum SysBrowserEventType {
		Unknown,
		Error,
		Closed,
		Opened,
		Foreground,
		Background
	}
	public static string GetDescription(this Enum value)
	{
		var field = value.GetType().GetField(value.ToString());
		if (field == null)
			return value.ToString();

		var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
		return attribute == null ? value.ToString() : attribute.Description;
	}
}
