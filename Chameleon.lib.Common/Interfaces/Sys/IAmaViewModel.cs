using Chameleon.lib.Common.Interfaces.Sys;

namespace Chameleon.lib.Common.Interfaces.Systemics;
public interface IAmaViewModel : IAmInitializer {
	string? Title { get; set; }
	Task OnNavigatedToAsync(object? param);
}