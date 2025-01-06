global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using System.Collections.Concurrent;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Configuration.Json;
global using Microsoft.Extensions.Configuration.EnvironmentVariables;

global using Chameleon.lib.Common.Extensions;
global using Chameleon.lib.Common.Types;
global using Chameleon.lib.Common.Managers;
global using Chameleon.lib.Common.Interfaces;
using Microsoft.Extensions.Logging.Console;
using System.Reflection;
using Chameleon.lib.Common.ServiceManagers;
using System.Text.Json;

namespace Chameleon.lib.Common;
public class IoC {
	private bool isInitialized = false;

	/// <summary>
	/// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
	/// </summary>
	public IServiceProvider? Services { get; private set; }

	/// <summary>
	/// Gets the <see cref="IChaonfigurationManager"/> instance to resolve application CONFIGURATIONS.
	/// </summary>
	public IChaonfigurationManager? Config { get; private set; }

	public void Init(Action<bool> action)
	{
		isInitialized = true;
		action(isInitialized);
	}

	/// <summary>
	/// Configures the services for the application.
	/// </summary>
	public void Configure(Func<WritableConfiguration> config, Action<ServiceCollection> action)
	{
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
	public static void SetValue<T>(T value, params string[] keys)
	{
		Instance.Config?.SetValue(string.Join('_', keys).Replace(' ', '_'), value);
		Toaster.Success("Settings saved");
	}
	public static void SetJsonValue<T>(T value, params string[] keys)
	{
		Instance.Config?.SetValue(string.Join('_', keys).Replace(' ', '_'), JsonSerializer.Serialize(value));
		Toaster.Success("Settings saved");
	}
	public static T? GetJsonValue<T>(params string[] keys) => GetValue<string>(string.Join('_', keys)) is string val ? JsonSerializer.Deserialize<T>(val) : default;
	public static Task SetValueAsync<T>(T value, params string[] keys) => Task.Run(() => SetValue(value, keys));
	//Singleton pattern
	public static IoC Instance { get; } = new IoC();
}
