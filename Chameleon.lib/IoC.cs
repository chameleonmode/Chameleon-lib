using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging.Console;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using Chameleon.lib.Helpers;
using Chameleon.lib.Services;
using Chameleon.lib.Util;
using Microsoft.Extensions.FileProviders;

namespace Chameleon.lib;

public class IoC {
	public const string AppName = "Chameleon";
	public const string SettingFile = "appsettings.json";
	public static readonly string Assembled = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0";
	private bool isInitialized = false;

	/// <summary>
	/// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
	/// </summary>
	public IServiceProvider? Services { get; private set; }

	/// <summary>
	/// Gets the <see cref="IConfiguration"/> instance to resolve application CONFIGURATIONS.
	/// </summary>
	public Configz? Config { get; private set; }

	/// <summary>
	/// List of al services tasks that need to be started on ioc init
	/// </summary>
	public List<IStartUp> StartUps { get; } = [];

	public async void Init(Action<bool> action) {
		foreach (var task in StartUps)
			await task.Start();

		isInitialized = true;
		action(isInitialized);
	}

	/// <summary>
	/// Configures the services for the application.
	/// </summary>
	public void Configure(Action<IConfigurationBuilder> config, Action<ServiceCollection> collection) {
		var builder = new ConfigurationBuilder();
		config(builder);
		Config = new Configz(new(builder
			.AddEnvironmentVariables()
			.AddJsonFile(SettingFile, optional: true, reloadOnChange: true)
			.Build()),
			Path.Combine(((PhysicalFileProvider)builder.GetFileProvider()).Root, SettingFile)
		);

		var services = new ServiceCollection();
		collection(services);

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
			 })
			 .BuildServiceProvider();
	}

	//
	public static T? GetService<T>() => (T?)I.Services?.GetService(typeof(T));
	public static T? GetService<T>(Type t) => (T?)I.Services?.GetService(t);
	public static object? GetService(Type t) => I.Services?.GetService(t);

	//
	public static string? GetValue(params string[] keys) => I.Config?.GetValue<string>(string.Join('_', keys));
	public static void SetValue(string key, string value) => I.Config?.SetValue(key, value);
	public static Task SetValue<T>(T value, params string[] keys) => Task.Run(() => {
		var key = string.Join('_', keys).Replace(' ', '_');
		if (
			I.Config is null ||
			EqualityComparer<T>.Default.Equals(I.Config.GetValue<T>(key), value)
		) return; // Value is unchanged; no update required.

		I.Config.SetValue(key, value, "Settings saved");
	});

	public static T? GetJsonValue<T>(params string[] keys) => JSON.Deserialize<T>(GetValue(keys) ?? "");
	public static void SetJsonValue<T>(T value, params string[] keys) => SetJsonVal(value, string.Join('_', keys).Replace(' ', '_'), "Settings saved");
	public static void SetJsonVal<T>(T value, string key, string? message = null) {
		if (
			I.Config is null ||
			JSON.Stringify(value) is not { } nv ||
			string.Equals(nv, I.Config.GetValue<string>(key), StringComparison.Ordinal)
		) return;
		I.Config.SetValue(key, nv, message);
	}

	//
	public static void ClearValue(params string[] keys) {
		if (I.Config is null) return;
		var key = string.Join('_', keys).Replace(' ', '_');
		_ = I.Config.overrides.TryRemove(key, out _);
		_ = SetValue("null", keys);
	}

	//Singleton pattern
	public static IoC I { get; } = new();
}
public class Configz(Configuration configuration, string filePath) {
	internal readonly ConcurrentDictionary<string, object> overrides = new();

	public T? GetValue<T>(string key) =>
		overrides.TryGetValue(key, out var value) ? (T)value : configuration.GetSection(key).Get<T>();

	public void SetValue<T>(string key, T value, string? message = null) {
		if (value is null) return;
		overrides[key] = value;
		configuration[key] = value?.ToString() ?? string.Empty;
		Save();
		if (message != null) Toaster.Success(message);
	}

	public void Save() {
		var data = configuration.AsEnumerable().ToDictionary(kv => kv.Key, kv => kv.Value);
		foreach (var kv in configuration.Store) data[kv.Key] = kv.Value;
		File.WriteAllText(filePath, JSON.Serialize(data));
	}
}

public class Configuration(IConfiguration configuration) : IConfiguration {
	public ConcurrentDictionary<string, string?> Store { get; } = new();
	public string? this[string key] {
		get => Store.TryGetValue(key, out var value) ? value : configuration[key];
		set => Store[key] = value;
	}

	public IChangeToken GetReloadToken() => configuration.GetReloadToken();
	public IConfigurationSection GetSection(string key) => configuration.GetSection(key) ?? new WritableSection(this, key);
	public IEnumerable<IConfigurationSection> GetChildren() =>
		configuration.GetChildren().Concat(
			Store.Keys.Select(k => k.Split(':')[0])
				.Except(configuration.GetChildren().Select(s => s.Key))
				.Select(k => new WritableSection(this, k)));

	class WritableSection(IConfiguration configuration, string key) : IConfigurationSection {
		public string Key { get; } = key;
		public string Path => Key;
		public string? Value { get => configuration[Key]; set => configuration[Key] = value; }
		public string? this[string key] { get => configuration[$"{Path}:{key}"]; set => configuration[$"{Path}:{key}"] = value; }
		public IChangeToken GetReloadToken() => configuration.GetReloadToken();
		public IConfigurationSection GetSection(string key) => new WritableSection(configuration, $"{Path}:{key}");
		public IEnumerable<IConfigurationSection> GetChildren() {
			var prefix = $"{Path}:";
			return configuration.GetChildren()
				.Where(c => c.Path.StartsWith(prefix))
				.Select(c => new WritableSection(configuration, c.Path[prefix.Length..]));
		}
	}
}

