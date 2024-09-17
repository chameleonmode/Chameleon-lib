using Chameleon.lib.Common.Util;
using Chameleon.lib.Core.Automation.Interfaces;
using Chameleon.lib.Core.Automation.Models;

namespace Chameleon.lib.Core.Automation.Services;
public class AutomationScriptRepository(IAutomationScriptApi apiClient)
: IAutomationScriptRepository {
		private readonly IAutomationScriptApi _client = apiClient;

		public void UpdateParameter(IAutomationScriptParameter param) => _client.UpdateParameter(param);

		public void SetParametersValue(IList<IAutomationParameterValue> values) => _client.SetParametersValue(values);

		public IList<IAutomationScriptDescription> GetAllScriptDescription() {
				var scriptDtos = _client.GetAllScriptDescription();
				var scripts = new List<IAutomationScriptDescription>();

				foreach (var script in scriptDtos) {
						var scriptDtoMapped = new AutomationScriptDescription {
								Title = script.Title,
								Description = script.Description,
								Parameters = []
						};

						foreach (var parameter in script.Parameters) {
								var parameterDtoMapped = new AutomationParameterValue {
										Id = parameter.Id,
										Name = parameter.Name,
										Value = parameter.Value,
								};
								scriptDtoMapped.Parameters.Add(parameterDtoMapped);
						}
						scripts.Add(scriptDtoMapped);
				}

				return scripts;
		}

	public Task<List<IAutomationScriptDescription>> GetAll(string filepath) => Task.Run(() => {
		var returned = new List<IAutomationScriptDescription>();
		foreach (var item in IOtil.ReadDirectory(filepath)) {
			var inf = new FileInfo(item);
			if (inf.Extension != ".cs")
				continue;
			returned.Add(new AutomationScriptDescription() {
				Title = inf.Name,
				Description = inf.Directory?.Name,
				FilePath = inf.FullName,
			});
		}
		return returned;
	});

	public IAutomationScript Get(int id) => _client.Get<AutomationScript>(id);

		public string GetScriptBody(int id) {
				var scriptBodyDto = _client.GetScriptBody(id);

				return scriptBodyDto;
		}

		public IList<IAutomationScript> BundledScripts { get; } = [];
}
