using Chameleon.lib.AIR.Scripts;
using Chameleon.lib.AIR.Scripts.Reddit.Post;
using Chameleon.lib.AIR.Scripts.Reddit.Subreddit;
using Chameleon.lib.Playwright.Scripts.CS;
using Chameleon.lib.Playwright.Scripts.JS.Reddit.Login;
using Chameleon.lib.Util;

namespace Chameleon.lib.Playwright.Services;

public record ScriptDescription(Dictionary<string, string> Parameters, string? Title = null, string? Description = null, string? FilePath = null);
public class BundledScriptsService {
	public IDictionary<string, IBundledCSScript> CsharpScripts { get; } = new Dictionary<string, IBundledCSScript> {
		{ nameof(KeepGmailAlive), new KeepGmailAlive() },
		{ nameof(URLsexplorer), new URLsexplorer() },
		{ nameof(GoogleCTR), new GoogleCTR() },
	};

	public IDictionary<string, IJSScript> BundledJSScripts { get; } = new Dictionary<string, IJSScript> {
		{ nameof(Comment), new Comment() },
		{ nameof(Reply), new Reply() },
		{ nameof(Join), new Join() },
		{ nameof(Post), new Post() },
		{ nameof(Vote), new Vote() },
		// TODO: { nameof(Google), new Google() },
		// TODO: { nameof(Credentials), new Credentials() },
		// OBSOLETE: { nameof(Gsites), new Gsites() },
	};

	public IList<Arguments> GetBundledScrits() {
		List<Arguments> AddMappedScripts<T>(IDictionary<string, T> scripts, Func<T, Arguments> createOptions) where T : IScript {
			return [.. scripts.Select(s => {
				 var description = new ScriptDescription (
					 Title: s.Value.Title,
					 Description: s.Value.Description,
					 FilePath: s.Value.File,
					 Parameters: s.Value.Args.ToDictionary(x => x.Key, x => x.Value)
				 );
				 var options = createOptions(s.Value);
				 options.Description = description;
				 return options;
			 })];
		}

		var returned = new List<Arguments>();
		// returned.AddRange(AddMappedScripts(BundledJSScripts, script => new Arguments { Script = script }));
		returned.AddRange(AddMappedScripts(CsharpScripts, script => new Arguments { Script = script }));

		return returned;
	}
	public static Task<IEnumerable<Arguments>> GetUserScripts() => Task.Run<IEnumerable<Arguments>>(() => {
		var path = IoC.GetValue("UserScriptsDirectory");
		if (path.Is() || !Directory.Exists(path)) return []; 
		
		var returned = new List<Arguments>();
		foreach (var item in IO.ReadDirectory(path)) {
			var inf = new FileInfo(item);
			if (inf.Extension != ".js")
				continue;
			returned.Add(new Arguments {
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
