using Chameleon.lib.Core.Automation.Interfaces;
using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chameleon.lib.Playwright.Interfaces;
public interface IBundledScript {
	string Title { get; }
	string Description { get; }
	IList<string> parameters { get; }
	Task Run(IBrowserContext browserContext, IList<IAutomationParameterValue>? pargs = null);
}
