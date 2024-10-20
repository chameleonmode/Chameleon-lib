using Chameleon.lib.Common;
using Chameleon.lib.Common.Interfaces.Services;
using Chameleon.lib.Common.Interfaces.Sys;

using CommunityToolkit.Mvvm.Input;

namespace Chameleon.lib.CommunityToolkit.MvvM;
public partial class ViewModelObjectBase : ObservableObjectBase {

	public bool Navigated { get; set; }

	public ViewModelObjectBase()
	{
		
	}

	public ViewModelObjectBase(string? title) : this()
	{
		Title = title;
	}

	public ViewModelObjectBase(string title, Func<ViewModelObjectBase> init) : this(title)
	{
		init();
	}

	[RelayCommand]
	private async Task Copy(object param)
	{
		var copyPastaService = IoC.GetService<ICopyPastaService>();
		if (copyPastaService == null)
			return;

		await copyPastaService.SetTextAsync(param as string ?? "");
	}
}