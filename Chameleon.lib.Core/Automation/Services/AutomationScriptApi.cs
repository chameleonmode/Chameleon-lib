using Chameleon.lib.Core.Automation.Interfaces;

namespace Chameleon.lib.Core.Automation.Services;
public class AutomationScriptApi
				: IAutomationScriptApi {

		public void UpdateParameter(IAutomationScriptParameter param) => throw new NotImplementedException();
		public void SetParametersValue(IList<IAutomationParameterValue> values) => throw new NotImplementedException();
		public IList<IAutomationScriptDescription> GetAllScriptDescription(object? query = null) => throw new NotImplementedException();
		public string GetScriptBody(int id) => throw new NotImplementedException();
		public T Get<T>(int id) => throw new NotImplementedException();
}
