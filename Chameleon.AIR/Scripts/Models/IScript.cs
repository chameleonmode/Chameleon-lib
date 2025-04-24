namespace Chameleon.AIR.Scripts.Models;

public interface IScript {
  string File { get; }
  string TableName { get; }
  string Title { get; }
  string Description { get; }
  IDictionary<string, string> Parameters { get; }
}

public interface IJSScript : IScript {
	Task<IDictionary<string,string>?> GetOptions(IDictionary<string,string>? options = null);
}