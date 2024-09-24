using System;

namespace Chameleon.lib.CommunityToolkit.MvvM;
public class PageViewModelBase : ObservableObjectBase {
	public PageViewModelBase()
	{
	}

	public PageViewModelBase(string? title) : this()
	{
		Title = title;
	}

	public PageViewModelBase(string title, Action init) : this(title)
	{
		init();
	}
}