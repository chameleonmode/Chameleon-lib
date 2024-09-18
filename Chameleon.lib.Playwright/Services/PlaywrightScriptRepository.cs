using Chameleon.lib.Common.Util;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;

namespace Chameleon.lib.Playwright.Services;
public class PlaywrightScriptRepository : IPlaywrightScriptRepository {
	public IDictionary<string, IBundledScript> BundledScripts { get; } = new Dictionary<string, IBundledScript> {
		{ nameof(GoogleCTRClickThrough), new GoogleCTRClickThrough() },
		{ nameof(KeepGmailAlive), new KeepGmailAlive() },
		{ nameof(URLsexplorer), new URLsexplorer() }
	};

	public Task<IList<PlaywrightScriptDescription>> GetAll(string filepath) => Task.Run<IList<PlaywrightScriptDescription>>(() => {
		var returned = new List<PlaywrightScriptDescription>();
		foreach (var item in IOtil.ReadDirectory(filepath)) {
			var inf = new FileInfo(item);
			if (inf.Extension != ".cs")
				continue;
			returned.Add(new PlaywrightScriptDescription() {
				Title = inf.Name,
				Description = inf.Directory?.Name,
				FilePath = inf.FullName,
			});
		}
		return returned;
	});
}
