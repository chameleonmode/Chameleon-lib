namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationScriptDescription {
		int Id { get; set; }
		string? Title { get; set; }
		string? Description { get; set; }
		string? FilePath { get; set; }
		IList<IAutomationParameterValue> Parameters { get; set; }
}
