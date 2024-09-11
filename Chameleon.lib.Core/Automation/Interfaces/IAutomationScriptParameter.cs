namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationScriptParameter {
		string Name { get; set; }
		int ScriptId { get; set; }
		IAutomationParameterValue Value { get; set; }
}
