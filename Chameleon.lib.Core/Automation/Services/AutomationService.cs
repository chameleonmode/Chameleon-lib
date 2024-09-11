using Chameleon.lib.Common.Util;
using Chameleon.lib.Core.Automation.Interfaces;
using Chameleon.lib.Core.Automation.Models;

namespace Chameleon.lib.Core.Automation.Services;
public class AutomationService(IAutomationScriptRepository repository)
				: IAutomationService {
		public Task<List<IAutomationScriptDescription>> GetAll() => Task.Run(() => {
				var entities = repository.GetAllScriptDescription();
				var response = new List<IAutomationScriptDescription>(entities);

				return response;
		});

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

		public Task UpdateParameter(IAutomationScriptParameter param) => Task.Run(() =>
						repository.UpdateParameter(param));

		public Task SetParametersValue(IList<IAutomationParameterValue> values) => Task.Run(() =>
						repository.SetParametersValue(values));

		public Task<string> GetScriptBody(int id) => Task.Run(() =>
						repository.GetScriptBody(id));

		public Task<string> GetScriptBody(string filepath) => File.Exists(filepath) ? File.ReadAllTextAsync(filepath) : Task.FromResult(string.Empty);
}

