namespace Chameleon.lib.Common.Interfaces.Services;

public interface IShowWindowService {
	void ShowTopmost<TView, TViewModel>(Action<TViewModel> initialize, Action<TViewModel>? OnClosed = null, string title = "TP", int width = 256) where TView : new() where TViewModel : new();
	void ShowTopmost<TView, TViewModel>(TViewModel vm, Action<TViewModel> initialize, Action<TViewModel>? OnClosed = null, string title = "TP", int width = 256) where TView : new();
	void ShowTopmost<TView, TViewModel>(TViewModel vm, TView v, Action<TViewModel> initialize, Action<TViewModel>? onClosed, string title = "TP", int width = 256);
}

