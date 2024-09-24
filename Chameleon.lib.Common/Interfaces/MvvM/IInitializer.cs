namespace Chameleon.lib.Common.Interfaces.MvvM;
public interface IInitializer {
	TaskCompletionSource LoadedTCS { get; }
	Task InvokeInitializeAsyncCommand(object? param);
}

