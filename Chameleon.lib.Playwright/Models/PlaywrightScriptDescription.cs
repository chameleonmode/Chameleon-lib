namespace Chameleon.lib.Playwright.Models;
public class PlaywrightScriptDescription {
	public int Id { get; set; } = -1;
	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? FilePath { get; set; }
	public IList<PlaywrightDescriptionParam> Parameters { get; set; } = [];
}
