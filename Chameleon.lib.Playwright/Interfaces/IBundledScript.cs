using Chameleon.AIR.Scripts.Models;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Interfaces;

public interface IBundledCSScript : IScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? options = null);
}
