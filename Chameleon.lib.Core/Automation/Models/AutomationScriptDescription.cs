using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Core.Automation.Models;
public class AutomationScriptDescription
					: IAutomationScriptDescription {
		public int Id { get; set; }
		public string? Title { get; set; }
		public string? Description { get; set; }
		public string? FilePath { get; set; }
		public IList<IAutomationParameterValue> Parameters { get; set; } = [];
}
