using Chameleon.lib.Common.Interfaces;

namespace Chameleon.lib.Core.Automation.Interfaces;

public interface IAutomationService
				: ISingletonDependency {
		Task<List<IAutomationScriptDescription>> GetAll();
		Task<List<IAutomationScriptDescription>> GetAll(string filepath);
		Task UpdateParameter(IAutomationScriptParameter param);
		Task SetParametersValue(IList<IAutomationParameterValue> values);
		Task<string> GetScriptBody(int id);
		Task<string> GetScriptBody(string filepath);
}
