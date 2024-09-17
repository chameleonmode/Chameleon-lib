using Microsoft.Extensions.Configuration;

namespace Chameleon.lib.Common.Interfaces;
public interface IChaonfigurationManager {
	T? GetValue<T>(string key);
	void SetValue<T>(string key, T value);
}

// This interface is not part of the standard IConfiguration, 
// it's just for demonstration purposes
public interface IWritableConfiguration : IConfiguration {
	new string? this[string key] { get; set; }
	void Save();
}
