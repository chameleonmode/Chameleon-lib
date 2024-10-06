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

	public enum MboxResult {
		/// <summary>
		/// No button was tapped.
		/// </summary>
		None = 0,

		/// <summary>
		/// The primary button was tapped by the user.
		/// </summary>
		Primary = 1,

		/// <summary>
		/// The secondary button was tapped by the user.
		/// </summary>
		Secondary = 2
	}

	public enum MBoxButtons {
		Ok,
		OkCancel,
		YesNo,
		YesNoCancel
	}

	public static string PrimaryBtnText(this MBoxButtons btns) => btns switch {
		MBoxButtons.Ok or MBoxButtons.OkCancel => "OK",
		MBoxButtons.YesNoCancel or MBoxButtons.YesNo => "Yes",
		_ => "OK"
	};

	public static string? SecondaryBtnText(this MBoxButtons btns) => btns switch {
		MBoxButtons.YesNoCancel => "No",
		_ => null
	};

	public static string? CloseBtnText(this MBoxButtons btns) => btns switch {
		MBoxButtons.YesNo => "No",
		MBoxButtons.YesNoCancel or
		MBoxButtons.OkCancel => "Cancel",
		_ => null
	};

	public static string GetDescription(this Enum value)
	{
		var field = value.GetType().GetField(value.ToString());
		if (field == null)
			return value.ToString();

		var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
		return attribute == null ? value.ToString() : attribute.Description;
	}
}
