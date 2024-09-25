using Chameleon.lib.Common.Interfaces.Systemics;

namespace Chameleon.lib.Common.Interfaces.Services;
public interface IToasterService : ISingletonDependency {
	void SetHostWindow(object? hostWindow);
	void ShowInformation(string message);
	void ShowError(string message);
	void ShowSuccess(string message);
	void ShowWarning(string message);
	void ClearAllMessages();
}
