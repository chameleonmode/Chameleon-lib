using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Core.Automation.Models;
public class AutomationParameterValue
				: IAutomationParameterValue {
		public int Id { get; set; }
		public string? Name { get; set; }
		public string? Value { get; set; }
		public int ParameterId { get; set; }
}
