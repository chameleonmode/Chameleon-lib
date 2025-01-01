using CommunityToolkit.Mvvm.ComponentModel;

namespace Chameleon.lib.CommunityToolkit.MvvM;
public abstract class Vim<T> : ViewModelObjectBase
	where T : Common.Models.Interfaces.Dto {
	public T? Dto { get; set; }

	public Vim(string? title) : base(title)
	{
	}
	public Vim(T dto) : base()
	{
		Dto = dto;
	}
	public Vim() : base()
	{
	}
}

public abstract partial class Obs<T> : Vim<T>
	where T : Common.Models.Interfaces.Dto {

	[ObservableProperty]
	private bool isSelected;

	[ObservableProperty]
	private bool isActionOptionsVisible = true;

	public Obs(string? title) : base(title)
	{
		CommandMap["Unselect"] = () => {
			IsSelected = false;
		};
	}

	partial void OnIsSelectedChanged(bool value)
	{
		OnAnyIsSelectedChanged(value);
	}

	public abstract void OnAnyIsSelectedChanged(bool value);
}
