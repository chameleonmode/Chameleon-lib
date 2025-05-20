namespace Chameleon.lib.Common.ServiceManagers;
public static class ServiceProvider {

	private static readonly Dictionary<Type, Func<object>> singletonFactories = [];
	private static readonly Dictionary<Type, Func<object>> transientFactories = [];
	private static readonly Dictionary<Type, object> singletonInstances = [];
	private static readonly object _lock = new();

	public static void RegisterSingleton<TInterface>(Func<TInterface> factory) where TInterface : class {
		lock (_lock) {
			var type = typeof(TInterface);
			singletonFactories[type] = () => factory();
			singletonInstances.Remove(type);
		}
	}

	public static void RegisterSingletonInstance<TInterface>(TInterface instance) where TInterface : class {
		lock (_lock) {
			var type = typeof(TInterface);
			singletonInstances[type] = instance ?? throw new ArgumentNullException(nameof(instance));
			singletonFactories.Remove(type);
		}
	}

	public static void RegisterSingleton<TInterface, TImplementation>()
			where TInterface : class
			where TImplementation : class, TInterface, new() {
		RegisterSingleton<TInterface>(() => new TImplementation());
	}

	public static void RegisterTransient<TInterface>(Func<TInterface> factory) where TInterface : class {
		lock (_lock) {
			transientFactories[typeof(TInterface)] = () => factory();
		}
	}

	public static void RegisterTransient<TInterface, TImplementation>()
			where TInterface : class
			where TImplementation : class, TInterface, new() {
		RegisterTransient<TInterface>(() => new TImplementation());
	}

	public static TInterface GetService<TInterface>() where TInterface : class {
		var type = typeof(TInterface);
		lock (_lock) {
			if (singletonInstances.TryGetValue(type, out var instance)) {
				return (TInterface)instance;
			}

			if (singletonFactories.TryGetValue(type, out var singletonFactory)) {
				var singletonInstance = (TInterface)singletonFactory()
					?? throw new InvalidOperationException($"Singleton factory for {type.FullName} returned null");
				singletonInstances[type] = singletonInstance;
				return singletonInstance;
			}

			if (transientFactories.TryGetValue(type, out var transientFactory)) {
				var transientInstance = (TInterface)transientFactory()
					?? throw new InvalidOperationException($"Transient factory for {type.FullName} returned null");
				return transientInstance;
			}
		}
		throw new InvalidOperationException($"Service not registered or unable to resolve: {type.FullName}");
	}


	public static void Reset() {
		lock (_lock) {
			singletonFactories.Clear();
			transientFactories.Clear();
			singletonInstances.Clear();
		}
	}
}