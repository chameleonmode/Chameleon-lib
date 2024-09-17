
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Xml;

using Chameleon.lib.Common.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Chameleon.lib.Common.Types;
public class WritableConfiguration(IConfiguration baseConfiguration, string filePath) : IWritableConfiguration {
	private readonly ConcurrentDictionary<string, string?> _writeStore = new();

	public string? this[string key] {
		get => _writeStore.TryGetValue(key, out var value) ? value : baseConfiguration[key];
		set => _writeStore[key] = value;
	}

	public IEnumerable<IConfigurationSection> GetChildren()
	{
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

	public void Save()
	{
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
