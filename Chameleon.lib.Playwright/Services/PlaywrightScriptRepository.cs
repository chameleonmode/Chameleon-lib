using Chameleon.lib.Common.Util;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;

namespace Chameleon.lib.Playwright.Services;
public class PlaywrightScriptRepository {
	public IDictionary<string, IBundledCSScript> BundledCSScripts { get; } = new Dictionary<string, IBundledCSScript> {
		{ nameof(GoogleCTRClickThrough), new GoogleCTRClickThrough() },
		{ nameof(KeepGmailAlive), new KeepGmailAlive() },
		{ nameof(URLsexplorer), new URLsexplorer() }
	};

	public IDictionary<string, IBundledJSScript> BundledJSScripts { get; } = new Dictionary<string, IBundledJSScript> {
		{ nameof(GsiteJsScript), new GsiteJsScript() },
		{ nameof(Reddit1Comment), new Reddit1Comment() },
	};

	public async Task<IList<RunScriptOptions>> GetAll(string filepath)
	{
		var returned = new List<RunScriptOptions>(await GetUserScripts(filepath));
		returned.AddRange(GetBundledScrits());
		return returned;
	}

	public IList<RunScriptOptions> GetBundledScrits()
	{
		List<RunScriptOptions> AddMappedScripts<T>(IDictionary<string, T> scripts, Func<T, RunScriptOptions> createOptions) where T : IBundledScript
		{
			 return [.. scripts.Select(s => {
				 var description = new PlaywrightScriptDescription (
					 Title: s.Value.Title,
					 Description: s.Value.Description,
					 FilePath: s.Value.Name,
					 Parameters: s.Value.Parameters.ToDictionary(x => x.Key, x => x.Value)
				 );
				 var options = createOptions(s.Value);
				 options.Description = description;
				 return options;
			 })];
		}

		var returned = new List<RunScriptOptions>();
		returned.AddRange(AddMappedScripts(BundledJSScripts, script => new RunScriptOptions { BundledJSScript = script }));
		// returned.AddRange(AddMappedScripts(BundledCSScripts, script => new PlaywriteRunScriptOptions { BundledCSScript = script }));

		return returned;
	}
	public Task<IList<RunScriptOptions>> GetUserScripts(string filepath) => Task.Run<IList<RunScriptOptions>>(() => {
		var returned = new List<RunScriptOptions>();
		foreach (var item in IOtil.ReadDirectory(filepath)) {
			var inf = new FileInfo(item);
			if (inf.Extension != ".js")
				continue;
			returned.Add(new RunScriptOptions {
				Description = new(
					Title: inf.Name,
					Description: inf.Directory?.Name ?? inf.FullName,
					FilePath: inf.FullName,
					Parameters: []
				),
			});
		}
		return returned;
	});

	// Singleton
	private static PlaywrightScriptRepository? _instance;
	public static PlaywrightScriptRepository Instance => _instance ??= new PlaywrightScriptRepository();
}
