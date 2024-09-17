using System.Collections.Generic;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IPlaywrightScriptRepository {
	IList<IBundledScript> BundledScripts { get; }
}
