namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationScript {
		IAutomationScriptDescription? AutomationScriptDescription { get; set; }
		string? Body { get; set; }
		IList<IAutomationScriptParameter> Parameters { get; set; }
}
