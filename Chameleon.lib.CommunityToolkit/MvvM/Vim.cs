namespace Chameleon.lib.CommunityToolkit.MvvM;
public abstract class Vim<T> : ViewModelObjectBase
	where T : Common.Models.Interfaces.Dto {
	public T? Dto { get; set; }

	public Vim(string? title) : base(title)
	{
	}
	public Vim() : base()
	{
	}
}
