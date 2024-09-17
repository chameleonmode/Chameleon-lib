using System.Collections.Generic;

using Chameleon.lib.Common.Util;
using Chameleon.lib.Core.Automation.Interfaces;
using Chameleon.lib.Core.Automation.Models;
using Chameleon.lib.Playwright.Interfaces;

namespace Chameleon.lib.Playwright.Services;
public class PlaywrightScriptRepository : IPlaywrightScriptRepository {
	public IList<IBundledScript> BundledScripts { get; } = [];

	public Task<List<IAutomationScriptDescription>> GetAll(string filepath) => Task.Run(() => {
		var returned = new List<IAutomationScriptDescription>();
		foreach (var item in IOtil.ReadDirectory(filepath)) {
			var inf = new FileInfo(item);
			if (inf.Extension != ".cs")
				continue;
			returned.Add(new AutomationScriptDescription() {
				Title = inf.Name,
				Description = inf.Directory?.Name,
				FilePath = inf.FullName,
			});
		}
		return returned;
	});
}
