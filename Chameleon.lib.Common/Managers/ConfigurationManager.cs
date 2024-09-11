namespace Chameleon.lib.Common.Managers;
public class ChaonfigurationManager(IConfiguration configuration) : IChaonfigurationManager {
		private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		private readonly ConcurrentDictionary<string, object> _overrides = new();

		public T? GetValue<T>(string key) {
				var returned = _overrides.TryGetValue(key, out var overriddenValue);
				return returned ? (T?)overriddenValue : _configuration.GetSection(key).Get<T>();
		}

		public void SetValue<T>(string key, T value)
		{
				ArgumentNullException.ThrowIfNull(value, nameof(value));
				_overrides[key] = value;

				// If the underlying configuration supports writing, update it as well
				if (_configuration is IWritableConfiguration writableConfig)
				{
						writableConfig[key] = value?.ToString() ?? string.Empty;
				}
		}
}
