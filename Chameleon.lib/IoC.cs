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
using Chameleon.lib.Services;
using System.Text.Json.Serialization;
using Chameleon.lib.Util;

namespace Chameleon.lib;

public static class Delegatorz {
	public delegate void Event<T>(object sender, T options);
}
public static class JSON {
	public static readonly JsonSerializerOptions CamelCaseOptions = new() {
		WriteIndented = true, // Pretty print JSON
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase, //Use camelCase
	};
	public static readonly JsonSerializerOptions CaseInsensitiveOptions = new() {
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
	};
	public static readonly JsonSerializerOptions InsensitiveCamelCaseOptions = new() {
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};
	public static readonly JsonSerializerOptions EnumConverter = new() {
		WriteIndented = true,
		IncludeFields = true,
		Converters = { new JsonStringEnumConverter() },
	};
	public static readonly JsonSerializerOptions InsensitiveEnumConverter = new() {
		 PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

	public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null) {
			return EX.Catch(()=> JsonSerializer.Deserialize<T>(json, options ?? InsensitiveCamelCaseOptions));
	}
	public static T Parse<T>(string? json, JsonSerializerOptions? options = null) {
		return Deserialize<T>(json ?? "", options) ?? Activator.CreateInstance<T>();
	}

	public static string Serialize(object o, JsonSerializerOptions? options = null) =>
	 JsonSerializer.Serialize(o, options ?? InsensitiveCamelCaseOptions);
	public static string? Stringify(object? o, JsonSerializerOptions? options = null) =>
	 o != null ? Serialize(o, options) : null;

	public class DynamicJsonConverter<T1, T2> : JsonConverter<T2> where T1 : T2 {
		public override T2? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;
			return JsonSerializer.Deserialize<T1>(jsonObject.GetRawText(), options);
		}

		public override void Write(Utf8JsonWriter writer, T2 value, JsonSerializerOptions options) {
			JsonSerializer.Serialize(writer, value, options);
		}
	}
}
public static class Const {
	public const string LocalHostUrl = "http://localhost:21021/api";
	public const string ApiBaseUrl = "https://api.chameleonmode.com/api";
	public const string NotionProfile = "https://www.notion.so/4-Setting-Up-Your-First-Profile-d2d001b2127e4a0e8e083fc13ad4cf99";
	public const string NotionUrl = "https://intercom.help/chameleonmode/en";
	public const string ApiSocialAnimalUrl = "https://api.socialanimal.com/api/v1/search";
	public const string WebsiteUrl = "https://chameleonmode.com/";
	public const string SupportUrl = "https://intercom.help/chameleonmode/en";
	public const string FacebookGroupUrl = "https://www.facebook.com/groups/962349154557466";
	public const string PricingUrl = "https://chameleonmode.com/pricing/";
	public const string DefaultHomePage = "https://example.com/";
	public const string AppName = "Chameleon";
	public const string AppSettingsFileName = "appsettings.json";
	public static readonly string Assembled = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0";
}
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
	
	/// <summary>
	/// List of al services tasks that need to be started on ioc init
	/// </summary>
	public List<IStartUp> StartUps { get; } = [];

	public async void Init(Action<bool> action)
	{
		foreach (var task in StartUps)
			await task.Start();
		
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
	public static T? GetService<T>() => (T?)Instance.Services?.GetService(typeof(T));
	public static object? GetService(Type t) => Instance.Services?.GetService(t);

	//
	public static T? GetValue<T>(string key) where T : class => Instance.Config?.GetValue<T>(key.Replace(' ', '_'));
	public static string? GetValue(params string[] keys) => GetValue<string>(string.Join('_', keys));
	//
	public static void SetValue<T>(T value, params string[] keys) {
		var key = string.Join('_', keys).Replace(' ', '_');
		var current = Instance.Config!.GetValue<T>(key);
		if (EqualityComparer<T>.Default.Equals(current, value)
			 || (value is Array arr && arr.Length == 0)) {
			return; // Value is unchanged; no update required.
		}

		Instance.Config!.SetValue(key, value);
		Toaster.Success("Settings saved");
	}
	//
	public static void SetJsonValue<T>(T value, params string[] keys) {
		var key = string.Join('_', keys).Replace(' ', '_');
		SetJsonVal(value, key, "Settings saved");
	}
	public static void SetJsonVal<T>(T value, string key, string? message = null) {
		var newValue = JsonSerializer.Serialize(value);
		var currentValue = Instance.Config!.GetValue<string>(key);
		if (string.Equals(newValue, currentValue, StringComparison.Ordinal)) {
			return; // Serialized JSON is unchanged; no update required.
		}
		Instance.Config!.SetValue(key, newValue);
		if (message != null)
			Toaster.Success(message);
	}
	public static T? GetJsonValue<T>(params string[] keys) => GetValue<string>(string.Join('_', keys)) is string val ? JsonSerializer.Deserialize<T>(val) : default;
	public static Task SetValueAsync<T>(T value, params string[] keys) => Task.Run(() => SetValue(value, keys));

	//
	public static void ClearValue(params string[] keys) {
		var key = string.Join('_', keys).Replace(' ', '_');
		if (Instance.Config is Chonfigurationer config) {
			_ = config._overrides.TryRemove(key, out _);
			SetValue("null", keys);
			Toaster.Success("Setting cleared");
		}
	}

	//Singleton pattern
	public static IoC Instance { get; } = new();
}

public class Chonfigurationer(IConfiguration configuration) {
	readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	internal readonly ConcurrentDictionary<string, object> _overrides = new();

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
						File.WriteAllText(filePath, JsonSerializer.Serialize(data));
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
