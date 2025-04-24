namespace Chameleon.AIR.Scripts.Models;

public abstract class JSScript(string SFile, string STitle, string SDescription) : IJSScript
{
  public string TableName { get; } = SFile.Replace("/", "_").Replace("-", "_").Replace(" ", "_");
  public string File { get; } = SFile;
  public string Title { get; } = STitle;
  public string Description { get; } = SDescription;

  public virtual IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>() {
    { "search" , "Search" }
  };

  public virtual Task<IDictionary<string, string>?> GetOptions(IDictionary<string, string>? options = null)
  {
    return Task.FromResult(options);
  }
}
