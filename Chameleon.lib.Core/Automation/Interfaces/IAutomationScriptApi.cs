using Chameleon.lib.Common.Interfaces;

namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationScriptApi
					: ISingletonDependency {
		void UpdateParameter(IAutomationScriptParameter param);
		void SetParametersValue(IList<IAutomationParameterValue> values);
		IList<IAutomationScriptDescription> GetAllScriptDescription(object? query = null);
		string GetScriptBody(int id);
		//TODO: 
		T Get<T>(int id);
}
