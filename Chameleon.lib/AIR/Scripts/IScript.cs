using System.Text.Json.Serialization;

namespace Chameleon.lib.AIR.Scripts;

public interface IScript {
  string File { get; }
  string Title { get; }
  string Description { get; }
  [JsonIgnore] string TableName { get; }
  [JsonIgnore] Dictionary<string, string> Args { get; }
}

public interface IJSScript : IScript {
  Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null);
}

public record Script(string File, string Title, string Description) : IScript {
  public string TableName { get; } = File.Replace("/", "_").Replace("-", "_").Replace(" ", "_");
  public virtual Dictionary<string, string> Args { get; init; } = [];
}

public record JSScript(string File, string Title, string Description) : Script(File, Title, Description), IJSScript {
  public virtual Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null) {
    throw new NotImplementedException("GetOptions must be implemented in derived classes.");
  }
}