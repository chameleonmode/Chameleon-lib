using System.ComponentModel;

namespace Chameleon.lib.Common.Enums;
public enum ExtensionType {
	[Description("cromeleon_addon")]
	chromeleon_addon,
	[Description("geo_addon")]
	geo_addon,
	[Description("chameleon_legacy")]
	chameleon_legacy,
	[Description("chromeleon_auto_proxy")]
	chromeleon_auto_proxy
}
