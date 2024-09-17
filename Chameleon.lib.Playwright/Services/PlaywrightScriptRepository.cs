using System.Collections.Generic;

using Chameleon.lib.Playwright.Interfaces;

namespace Chameleon.lib.Playwright.Services;
public class PlaywrightScriptRepository : IPlaywrightScriptRepository {
	public IList<IBundledScript> BundledScripts { get; } = [];
}
