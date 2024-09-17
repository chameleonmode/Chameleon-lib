
using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Core.Automation.Models;
public class AutomationScriptParameter : IAutomationScriptParameter {
	public string? Name { get; set; }
	public int ScriptId { get; set; }
	public IAutomationParameterValue? Value { get; set; }
}
