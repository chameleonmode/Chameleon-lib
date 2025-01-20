using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

using Microsoft.Extensions.Logging.Console;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using Chameleon.lib.Helpers;

namespace Chameleon.lib;
public class IoC {
	private bool isInitialized = false;

	/// <summary>
	/// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
	/// </summary>
	public IServiceProvider? Services { get; private set; }

	/// <summary>
	/// Gets the <see cref="IChaonfigurationManager"/> instance to resolve application CONFIGURATIONS.
	/// </summary>
	public Chonfigurationer? Config { get; private set; }

	public void Init(Action<bool> action) {
		isInitialized = true;
		action(isInitialized);
	}

	/// <summary>
	/// Configures the services for the application.
	/// </summary>
	public void Configure(Func<WritableConfiguration> config, Action<ServiceCollection> action) {
		Config = new Chonfigurationer(config());

		var services = new ServiceCollection();
		action(services);

		Services = services
						.AddLogging(builder => {
							_ = builder
									.AddConsole(opt => {
										opt.FormatterName = ConsoleFormatterNames.Simple;
									})
									.AddFilter(level => true)
									.SetMinimumLevel(LogLevel.Trace);
						})
						.Configure<LoggerFilterOptions>(options => {
							options.MinLevel = LogLevel.Trace;
							options.CaptureScopes = true;
						})
						.Configure<SimpleConsoleFormatterOptions>(options => {
							options.IncludeScopes = true;
						}).BuildServiceProvider();
	}

	//
	// Summary:
	//     Get service of type T from the System.IServiceProvider.
	//
	// Parameters:
	//   provider:
	//     The System.IServiceProvider to retrieve the service object from.
	//
	// Type parameters:
	//   T:
	//     The type of service object to get.
	//
	// Returns:
	//     A service object of type T or null if there is no such service.
	public static T? GetService<T>() => (T?)Instance.Services?.GetService(typeof(T));
	public static object? GetService(Type t) => Instance.Services?.GetService(t);

	public static T? GetValue<T>(string key) => Instance.Config == null ? throw new ArgumentException("Configuration manager is not initialized", nameof(key)) : Instance.Config.GetValue<T>(key.Replace(' ', '_'));
	public static string? GetValue(params string[] keys) => GetValue<string>(string.Join('_', keys));
	public static void SetValue<T>(T value, params string[] keys) {
		Instance.Config?.SetValue(string.Join('_', keys).Replace(' ', '_'), value);
		Toaster.Success("Settings saved");
	}
	public static void SetJsonValue<T>(T value, params string[] keys) {
		Instance.Config?.SetValue(string.Join('_', keys).Replace(' ', '_'), JsonSerializer.Serialize(value));
		Toaster.Success("Settings saved");
	}
	public static T? GetJsonValue<T>(params string[] keys) => GetValue<string>(string.Join('_', keys)) is string val ? JsonSerializer.Deserialize<T>(val) : default;
	public static Task SetValueAsync<T>(T value, params string[] keys) => Task.Run(() => SetValue(value, keys));
	//Singleton pattern
	public static IoC Instance { get; } = new IoC();
}

public class Chonfigurationer(IConfiguration configuration) {
	private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	private readonly ConcurrentDictionary<string, object> _overrides = new();

	public T? GetValue<T>(string key) {
		var returned = _overrides.TryGetValue(key, out var overriddenValue);
		return returned ? (T?)overriddenValue : _configuration.GetSection(key).Get<T>();
	}

	public void SetValue<T>(string key, T value) {
		ArgumentNullException.ThrowIfNull(value, nameof(value));
		_overrides[key] = value;

		// If the underlying configuration supports writing, update it as well
		if (_configuration is WritableConfiguration writableConfig) {
			writableConfig[key] = value?.ToString() ?? string.Empty;
			writableConfig.Save();
		}
	}
}

public class WritableConfiguration(IConfiguration baseConfiguration, string filePath) : IConfiguration {
	private readonly ConcurrentDictionary<string, string?> _writeStore = new();

	public string? this[string key] {
		get => _writeStore.TryGetValue(key, out var value) ? value : baseConfiguration[key];
		set => _writeStore[key] = value;
	}

	public IEnumerable<IConfigurationSection> GetChildren() {
		var baseSections = baseConfiguration.GetChildren();
		var writtenKeys = _writeStore.Keys.Select(k => k.Split(':')[0]).Distinct();

		return baseSections
			.Concat(writtenKeys
				.Except(baseSections
					.Select(s => s.Key))
				.Select(k => new WritableConfigurationSection(this, k)))
			.DistinctBy(s => s.Key);
	}

	public IChangeToken GetReloadToken() => baseConfiguration.GetReloadToken();

	public IConfigurationSection GetSection(string key) => baseConfiguration.GetSection(key) ?? new WritableConfigurationSection(this, key);

	public void Save() {
		var jsonConfig = baseConfiguration as IConfigurationRoot;
		if (jsonConfig != null) {
			var jsonProvider = jsonConfig.Providers.FirstOrDefault(p => p is JsonConfigurationProvider) as JsonConfigurationProvider;
			if (jsonProvider != null) {
				var field = typeof(JsonConfigurationProvider).GetProperty("Data", BindingFlags.NonPublic | BindingFlags.Instance);
				if (field != null) {
					var data = field.GetValue(jsonProvider) as IDictionary<string, string?>;
					if (data != null) {
						foreach (var kvp in _writeStore) {
							data[kvp.Key] = kvp.Value;
						}

						var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
						File.WriteAllText(filePath, json);
					}
				}
			}
		}
	}

	private class WritableConfigurationSection(WritableConfiguration configuration, string key) : IConfigurationSection {
		private readonly WritableConfiguration _configuration = configuration;

		public string? this[string key] {
			get => _configuration[$"{Path}:{key}"];
			set => _configuration[$"{Path}:{key}"] = value;
		}

		public string Key { get; } = key;
		public string Path => Key;
		public string? Value {
			get => _configuration[Key];
			set => _configuration[Key] = value;
		}

		public IEnumerable<IConfigurationSection> GetChildren() =>
			_configuration.GetChildren()
			.Where(c => c.Path.StartsWith($"{Path}:"))
			.Select(c => new WritableConfigurationSection(_configuration, c.Path[(Path.Length + 1)..]));

		public IChangeToken GetReloadToken() => _configuration.GetReloadToken();

		public IConfigurationSection GetSection(string key) =>
			new WritableConfigurationSection(_configuration, $"{Path}:{key}");
	}
}
