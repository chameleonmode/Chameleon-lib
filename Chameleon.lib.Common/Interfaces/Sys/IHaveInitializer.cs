namespace Chameleon.lib.Common.Interfaces.Sys;
public interface IAmInitializer {
	TaskCompletionSource<bool> LoadedTCS { get; }
	Task InitializeAsync(object? param = null);
}
