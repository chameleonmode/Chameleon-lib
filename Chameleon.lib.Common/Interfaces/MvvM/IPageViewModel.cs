namespace Chameleon.lib.Common.Interfaces.MvvM;
public interface IAmViewModel : IInitializer {
	string Title { get; set; }
	Task OnNavigatedToAsync(object? param);
}
