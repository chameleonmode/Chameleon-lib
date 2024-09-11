namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationParameterValue {
		int ParameterId { get; set; }
		string? Name { get; set; }
		string? Value { get; set; }
		int Id { get; set; }
}