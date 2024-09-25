namespace Chameleon.lib.WebBrowser.Models;
public class EmulationOptions {
	public bool AutoTimezone { get; set; }
	public bool DissableHyperlinkAuditing { get; set; }
	public bool DNT { get; set; }
	public bool SpoofWebGLFingerprint { get; set; }
	public bool SpoofCanvasFingerprint { get; set; }
	public bool SpoofClientRects { get; set; }
	public bool SpoofFontFingerprint { get; set; }
	public bool DisableWebRTC { get; set; }
	public bool SpoofGeoLocation { get; set; }
}
