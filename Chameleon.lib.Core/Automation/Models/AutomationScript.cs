using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Core.Automation.Models;
internal class AutomationScript : IAutomationScript {
		public string? Title { get; set; }
		public string? Description { get; set; }
		public string? Body { get; set; }
		public IList<IAutomationScriptParameter> Parameters { get; set; } = [];
		public IAutomationScriptDescription? AutomationScriptDescription { get; set; }
}
