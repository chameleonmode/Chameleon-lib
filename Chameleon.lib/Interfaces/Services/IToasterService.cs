namespace Chameleon.lib.Interfaces.Services;
public interface IToasterService {
	void SetHostWindow(object? hostWindow);
	void ShowInformation(string message);
	void ShowError(string message);
	void ShowSuccess(string message);
	void ShowWarning(string message);
	void ClearAllMessages();
}
