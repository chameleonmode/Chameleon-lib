using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Interfaces.Services;
using Chameleon.lib.Const;

namespace Chameleon.lib.Common.ServiceManagers;
public class Mbox {
	private readonly IMboxService? MboxService;

	public static async Task<bool> Show(string title, string content, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo, string? fontIconInfo = null, Enums.MboxResult retVal = Enums.MboxResult.Primary)
	{
		return await Instance.MboxService!.Show(title, content, btns, fontIconInfo ?? "Info") == retVal;
	}
	public static Task<bool> ShowErrorAsync(string title, string content)
	{
		return Show(title, content, Enums.MBoxButtons.Ok, "Error");
	}

	public static Task<Enums.TaskDialogResult> ShowTaskDialog<TViewModel, TView>(Func<TViewModel> initialize, string header,
		string? subHeader = null, string title = Variables.AppName, object? footer = null, 
		Enums.Symbas symbas = Enums.Symbas.Alert, Enums.MBoxButtons btns = Enums.MBoxButtons.YesNo) where TView : new()
	{
		return Instance.MboxService!.ShowTaskDialog(initialize, new TView(), header, subHeader, title, footer, symbas, btns);
	}

	public static Task<Enums.MboxResult> ShowContentDialog<TView, TViewModel>(Action<TViewModel> initialize)
	{
		return Instance.MboxService!.ShowContentDialog<TView, TViewModel>(initialize);
	}
	public static Mbox Instance { get; } = new Mbox();
	private Mbox()
	{
		MboxService = IoC.GetService<IMboxService>();
	}
}
