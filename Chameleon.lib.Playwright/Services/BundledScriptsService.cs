using Chameleon.lib.Common.Util;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts.CS;
using Chameleon.lib.Playwright.Scripts.JS;

namespace Chameleon.lib.Playwright.Services;
public class BundledScriptsService {
	public IDictionary<string, IBundledCSScript> BundledCSScripts { get; } = new Dictionary<string, IBundledCSScript> {
		{ nameof(GoogleCTR), new GoogleCTR() },
		{ nameof(KeepGmailAlive), new KeepGmailAlive() },
		{ nameof(URLsexplorer), new URLsexplorer() }
	};

	public IDictionary<string, IBundledJSScript> BundledJSScripts { get; } = new Dictionary<string, IBundledJSScript> {
		{ nameof(Gsites), new Gsites() },
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
		returned.AddRange(AddMappedScripts(BundledJSScripts, script => new RunScriptOptions { BundledScript = script }));
		returned.AddRange(AddMappedScripts(BundledCSScripts, script => new RunScriptOptions { BundledScript = script }));

		return returned;
	}
	public static Task<IList<RunScriptOptions>> GetUserScripts(string filepath) => Task.Run<IList<RunScriptOptions>>(() => {
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
	private static BundledScriptsService? _instance;
	public static BundledScriptsService Instance => _instance ??= new BundledScriptsService();
}
