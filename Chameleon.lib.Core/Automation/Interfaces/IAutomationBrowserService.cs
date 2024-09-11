using Chameleon.lib.Common.Interfaces;

namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationBrowserService
				: ISingletonDependency {
		Task RunScript(
						IAutomationRunScriptOptions options,
						CancellationToken token);
}
