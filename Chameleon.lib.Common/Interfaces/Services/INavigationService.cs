namespace Chameleon.lib.Common.Interfaces.Services;
public interface INavigationService {
	Task PopAsync();
	void SetFrame(object f); //TODO: change to actual
	void SetOverlayHost(object p); //TODO: change to actual
	void NavigateToType(Type t, object? parameter = null);
	void NavigateFromContext(object dataContext);
	void ClearOverlay();
}
