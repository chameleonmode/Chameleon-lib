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

namespace Chameleon.lib.Common;
public class IoC {
		private bool isInitialized = false;
		private IoC() {
				Services = Configure();
				Config = CreateConfigurationManager();
				isInitialized = true;
		}

		/// <summary>
		/// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
		/// </summary>
		public IServiceProvider Services { get; }

		/// <summary>
		/// Gets the <see cref="IChaonfigurationManager"/> instance to resolve application CONFIGURATIONS.
		/// </summary>
		public IChaonfigurationManager Config { get; }

		public void Init(Action<bool> action) {
				isInitialized = true;
				action(isInitialized);
		}

		/// <summary>
		/// Configures the services for the application.
		/// </summary>
		private static ServiceProvider Configure() {
				var services = new ServiceCollection();

				_ = services
						.AddLogging(configure => configure.AddConsole());

				return services.BuildServiceProvider();
		}

		public static IChaonfigurationManager CreateConfigurationManager() {
				// Setup
				var baseConfig = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
								.AddEnvironmentVariables()
								.Build();

				var writableConfig = new WritableConfiguration(baseConfig);
				return new ChaonfigurationManager(writableConfig);
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
		public static T? GetService<T>() => (T?)Instance.Services.GetService(typeof(T));

		public static T? GetValue<T>(string key) => Instance.Config.GetValue<T>(key);

		//Singleton pattern
		public static IoC Instance { get; } = new IoC();
}
