using System.Collections.Specialized;

using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Interfaces;

namespace Chameleon.lib.Core.Interfaces;
public interface IAutomationRunScriptOptions {
		int Port { get; set; }
		bool Record { get; set; }
		SystemBrowserType BrowserType { get; set; }
		IAutomationScriptDescription Script { get; set; }
}

public interface IAutomationParameterValue {
		int ParameterId { get; set; }
		string Name { get; set; }
		string Value { get; set; }
}

public interface IAutomationScriptDescription
				: IReadOnlyList<IAutomationScriptDescription>
				, INotifyCollectionChanged {
		string Title { get; set; }
		string Description { get; set; }
		string FilePath { get; set; }
		IList<IAutomationParameterValue> Parameters { get; set; }
}

public interface IAutomationBrowserService
				: ISingletonDependency {
		Task RunScript(
						IAutomationRunScriptOptions options,
						CancellationToken token);
}
