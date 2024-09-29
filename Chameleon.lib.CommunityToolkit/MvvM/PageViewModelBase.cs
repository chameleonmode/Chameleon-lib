namespace Chameleon.lib.CommunityToolkit.MvvM;
public class ViewModelObjectBase : ObservableObjectBase {
	public ViewModelObjectBase()
	{
	}

	public ViewModelObjectBase(string? title) : this()
	{
		Title = title;
	}

	public ViewModelObjectBase(string title, Action init) : this(title)
	{
		init();
	}
}