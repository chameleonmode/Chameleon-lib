namespace Chameleon.AIR.Scripts.Models;

public interface IScript {
  string File { get; }
  string TableName { get; }
  string Title { get; }
  string Description { get; }
  Dictionary<string, string> Args { get; }
}

public interface IJSScript : IScript {
  Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null);
}


public abstract record JSScript(string File, string Title, string Description) : IJSScript {
  public string TableName { get; } = File.Replace("/", "_").Replace("-", "_").Replace(" ", "_");

  public virtual Dictionary<string, string> Args { get; init; } = [];

  public virtual Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null) {
    return Task.FromResult(options);
  }
}