using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Util;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Models;
using Chameleon.lib.Playwright.Scripts;

namespace Chameleon.lib.Playwright.Services;
public class PlaywrightScriptRepository : IPlaywrightScriptRepository {
	public IDictionary<string, IBundledCSScript> BundledCSScripts { get; } = new Dictionary<string, IBundledCSScript> {
		{ nameof(GoogleCTRClickThrough), new GoogleCTRClickThrough() },
		{ nameof(KeepGmailAlive), new KeepGmailAlive() },
		{ nameof(URLsexplorer), new URLsexplorer() }
	};

	public IDictionary<string, IBundledJSScript> BundledJSScripts { get; } = new Dictionary<string, IBundledJSScript> {
		{ nameof(GsiteJsScript), new GsiteJsScript() }
	};

	public async Task<IList<PlaywriteRunScriptOptions>> GetAll(string filepath)
	{
		var returned = new List<PlaywriteRunScriptOptions>(await GetUserScripts(filepath));
		returned.AddRange(GetBundledScrits());
		return returned;
	}

	public IList<PlaywriteRunScriptOptions> GetBundledScrits()
	{
		List<PlaywriteRunScriptOptions> AddMappedScripts<T>(IDictionary<string, T> scripts, Func<T, PlaywriteRunScriptOptions> createOptions) where T : IBundledScript
		{
			 return scripts.Select(s => {
				 var description = new PlaywrightScriptDescription {
					 Title = s.Value.Title,
					 Description = s.Value.Description,
					 Parameters = s.Value.Parameters
							.Select(p => new PlaywrightDescriptionParam { Key = p, Value = IoC.GetValue<string>($"{s.Value.Title} {p}") ?? string.Empty })
							.ToList()
				 };
				 var options = createOptions(s.Value);
				 options.Description = description;
				 return options;
			 }).ToList();
		}

		var returned = new List<PlaywriteRunScriptOptions>();
		returned.AddRange(AddMappedScripts(BundledCSScripts, script => new PlaywriteRunScriptOptions { BundledCSScript = script }));
		returned.AddRange(AddMappedScripts(BundledJSScripts, script => new PlaywriteRunScriptOptions { BundledJSScript = script }));

		return returned;
	}
	public Task<IList<PlaywriteRunScriptOptions>> GetUserScripts(string filepath) => Task.Run<IList<PlaywriteRunScriptOptions>>(() => {
		var returned = new List<PlaywriteRunScriptOptions>();
		foreach (var item in IOtil.ReadDirectory(filepath)) {
			var inf = new FileInfo(item);
			if (inf.Extension != ".cs")
				continue;
			returned.Add(new PlaywriteRunScriptOptions {
				Description = new PlaywrightScriptDescription() {
					Title = inf.Name,
					Description = inf.Directory?.Name,
					FilePath = inf.FullName,
				},
			});
		}
		return returned;
	});
}
