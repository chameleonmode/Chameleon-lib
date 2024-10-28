using Chameleon.lib.Common.Interfaces.Systemics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chameleon.lib.CommunityToolkit.MvvM;

public abstract partial class ObservableObjectBase : ObservableObject,
		IAmaViewModel {
	[ObservableProperty]
	private string? title;

	[ObservableProperty]
	private bool loaded;

	private long _isBusy;
	public bool IsBusy => Interlocked.Read(ref _isBusy) > 0;

	public virtual Dictionary<string, Action> CommandMap { get; } = [];
	public virtual Dictionary<string, Func<Task>> AsyncCommandMap { get; } = [];

	public IAsyncRelayCommand InitializeAsyncCommand { get; }
	public TaskCompletionSource<bool> LoadedTCS { get; } = new();

	public ObservableObjectBase()
	{
		InitializeAsyncCommand = new AsyncRelayCommand<object>(
				async (p) => {
					_ = Interlocked.Increment(ref _isBusy);
					OnPropertyChanged(nameof(IsBusy));

					try {
						await InitAsync(p);
					} finally {
						_ = Interlocked.Decrement(ref _isBusy);
						OnPropertyChanged(nameof(IsBusy));
					}
					Loaded = true;
					_ = LoadedTCS.TrySetResult(false);
				},
				AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler);
	}
	public virtual Task InitAsync(object? param) => Task.CompletedTask;
	public virtual Task OnNavigatedToAsync(object? param) => Task.CompletedTask;

	public Task InvokeInitializeAsyncCommand(object? p = null) => InitializeAsyncCommand.ExecuteAsync(p);

	[RelayCommand]
	public void CfromV(string what) => CommandMap[what]?.Invoke();

	[RelayCommand]
	public async Task AsyncCfromV(string what) => await AsyncCommandMap[what]();
	public Task InitializeAsync(object? param) => InvokeInitializeAsyncCommand(param);
}
