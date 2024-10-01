using Chameleon.lib.Common;
using Chameleon.lib.Common.Interfaces.Services;

using CommunityToolkit.Mvvm.Input;

namespace Chameleon.lib.CommunityToolkit.MvvM;
public partial class ViewModelObjectBase : ObservableObjectBase {

	private readonly ICopyPastaService? copyPastaService;

	public ViewModelObjectBase()
	{
		copyPastaService = IoC.GetService<ICopyPastaService>();
	}

	public ViewModelObjectBase(string? title) : this()
	{
		Title = title;
	}

	public ViewModelObjectBase(string title, Action init) : this(title)
	{
		init();
	}

	[RelayCommand]
	private async Task Copy(object param)
	{
		if(copyPastaService == null)
			return;

		await copyPastaService.SetTextAsync(param as string ?? "");
	}
}