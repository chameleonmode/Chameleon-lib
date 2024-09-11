namespace Chameleon.lib.Core.Automation.Interfaces;
public interface IAutomationScriptRepository {
		void UpdateParameter(IAutomationScriptParameter param);
		void SetParametersValue(IList<IAutomationParameterValue> values);
		IList<IAutomationScriptDescription> GetAllScriptDescription();
		string GetScriptBody(int id);
}

