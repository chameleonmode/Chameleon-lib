using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;
using Chameleon.lib.Const;

namespace Chameleon.lib.Common.ServiceManagers;

public class Mbox {
	private readonly IMboxService? MboxService;

	public static async Task<bool> Show(string title, string content, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo, string? fontIconInfo = null, Enums.MboxResult retVal = Enums.MboxResult.Primary) {
		return await Instance.MboxService!.Show(title, content, btns, fontIconInfo ?? "Info") == retVal;
	}
	public static Task<bool> ShowErrorAsync(string title, string content) {
		return Show(title, content, Enums.MBoxButtons.Ok, "Error");
	}

	public record TaskDialogParameters<TViewModel>(
		Func<TViewModel> Initialize,
		string Header,
		string? SubHeader = null,
		string Title = Variables.AppName,
		object? Footer = null,
		Enums.Symbas Symbas = Enums.Symbas.Alert,
		Enums.MBoxButtons Btns = Enums.MBoxButtons.YesNo
	);

	public static Task<Enums.TaskDialogResult> ShowTaskDialog<TView, TViewModel>(
		TaskDialogParameters<TViewModel> parameters
	) where TView : new() {
		return Instance.MboxService!.ShowTaskDialog(parameters.Initialize, new TView(), parameters.Header, parameters.SubHeader, parameters.Title, parameters.Footer, parameters.Symbas, parameters.Btns);
	}

	public static Mbox Instance { get; } = new Mbox();
	private Mbox() {
		MboxService = IoC.GetService<IMboxService>();
	}
}
