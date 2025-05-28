using System.ComponentModel;

namespace Chameleon.lib.Common.Constants;

public static class Enums {
	public static class Api {
		public enum ProxyHostType {
			IpAddress,
			Hostname
		}
		public enum ProxyIpType {
			Random,
			Sticky
		}
		public enum ProxyProtocolType {
			Http,
			Ssl
		}
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



	public static string GetDescription(this Enum value) {
		var field = value.GetType().GetField(value.ToString());
		if (field == null)
			return value.ToString();

		var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
		return attribute == null ? value.ToString() : attribute.Description;
	}

	public enum ChangeComparereOption {
		Ascending,
		Descending
	}

	public enum GenderType {
		Unknown,
		Male,
		Female
	}

}
