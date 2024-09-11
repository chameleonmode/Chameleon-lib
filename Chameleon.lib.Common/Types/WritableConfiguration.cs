
using System.Collections.Concurrent;

using Chameleon.lib.Common.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Chameleon.lib.Common.Types;
public class WritableConfiguration(IConfiguration baseConfiguration) : IWritableConfiguration {
		private readonly ConcurrentDictionary<string, string?> _writeStore = new();

		public string? this[string key] {
				get => _writeStore.TryGetValue(key, out var value) ? value : null;
				set => _writeStore[key] = value;
		}

		public IEnumerable<IConfigurationSection> GetChildren() {
				var baseSections = baseConfiguration.GetChildren();
				var writtenKeys = _writeStore.Keys.Select(k => k.Split(':')[0]).Distinct();

				return baseSections.Concat(writtenKeys.Except(baseSections.Select(s => s.Key))
																																										.Select(k => new WritableConfigurationSection(this, k)))
																							.DistinctBy(s => s.Key);
		}

		public IChangeToken GetReloadToken() => baseConfiguration.GetReloadToken();

		public IConfigurationSection GetSection(string key) => baseConfiguration.GetSection(key) ?? new WritableConfigurationSection(this, key);

		private class WritableConfigurationSection(WritableConfiguration configuration, string key) : IConfigurationSection {
				private readonly WritableConfiguration _configuration = configuration;

				public string? this[string key] {
						get => _configuration[!Path.Is() ? key : $"{Path}:{key}"];
						set => _configuration[!Path.Is() ? key : $"{Path}:{key}"] = value;
				}

				public string Key { get; } = key;
				public string Path => Key;
				public string? Value {
						get => _configuration[Key];
						set => _configuration[Key] = value;
				}

				public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren()
																											.Where(c => c.Path.StartsWith($"{Path}:"))
																											.Select(c => new WritableConfigurationSection(_configuration, c.Path[(Path.Length + 1)..]));

				public IChangeToken GetReloadToken() => _configuration.GetReloadToken();

				public IConfigurationSection GetSection(string key) => new WritableConfigurationSection(_configuration, string.IsNullOrEmpty(Path) ? key : $"{Path}:{key}");
		}
}
