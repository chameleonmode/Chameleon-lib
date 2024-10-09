namespace Chameleon.lib.WebBrowser.Models;
public class EmulationOptions {
	public bool AutoTimezone { get; set; } = true;
	public bool SpoofWebGLFingerprint { get; set; } = true;
	public bool SpoofCanvasFingerprint { get; set; } = true;
	public bool SpoofClientRects { get; set; } = true;
	public bool SpoofFontFingerprint { get; set; } = true;
	public bool DisableWebRTC { get; set; } = true;
	public bool SpoofGeoLocation { get; set; } = true;
}
