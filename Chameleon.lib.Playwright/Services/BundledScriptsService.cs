using Chameleon.lib.Common.Util;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts.CS;
using Chameleon.lib.Playwright.Scripts.JS;
using Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;
using Chameleon.lib.Playwright.Scripts.JS.Reddit.Post;
using Chameleon.lib.Playwright.Scripts.JS.Reddit.Subreddit;

namespace Chameleon.lib.Playwright.Services;

public class BundledScriptsService {
	public IDictionary<string, IBundledCSScript> BundledCSScripts { get; } = new Dictionary<string, IBundledCSScript> {
		{ nameof(KeepGmailAlive), new KeepGmailAlive() },
		{ nameof(URLsexplorer), new URLsexplorer() },
		{ nameof(GoogleCTR), new GoogleCTR() },
	};

	public IDictionary<string, IBundledJSScript> BundledJSScripts { get; } = new Dictionary<string, IBundledJSScript> {
		{ nameof(CommentOnTitle), new CommentOnTitle() },
		{ nameof(ReplyToComment), new ReplyToComment() },
		{ nameof(Join), new Join() },
		{ nameof(Post), new Post() },
		{ nameof(Vote), new Vote() },
		{ nameof(Google), new Google() },
		{ nameof(Credentials), new Credentials() },
		{ nameof(Gsites), new Gsites() },
	};

	public async Task<IList<RunScriptOptions>> GetAll(string filepath) {
		var returned = new List<RunScriptOptions>(await GetUserScripts(filepath));
		returned.AddRange(GetBundledScrits());
		return returned;
	}

	public IList<RunScriptOptions> GetBundledScrits() {
		List<RunScriptOptions> AddMappedScripts<T>(IDictionary<string, T> scripts, Func<T, RunScriptOptions> createOptions) where T : IBundledScript {
			return [.. scripts.Select(s => {
				 var description = new PlaywrightScriptDescription (
					 Title: s.Value.Title,
					 Description: s.Value.Description,
					 FilePath: s.Value.File,
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
