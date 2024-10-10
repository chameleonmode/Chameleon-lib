using Chameleon.lib.Common.Constants;

namespace Chameleon.lib.Common.Interfaces.Services;
public interface IMboxService {
	Task<Enums.MboxResult> Show(string title,
		string content,
		Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo,
		string icon = "Info");

  Task<Enums.TaskDialogResult> ShowTaskDialog<TViewModel>(Func<TViewModel> initialize, object content, string header, string? subHeader = null, string title = Consts.AppName, object? footer = null, Enums.Symbas symbas = Enums.Symbas.Alert, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo);
}
