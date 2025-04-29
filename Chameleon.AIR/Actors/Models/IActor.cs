using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Actors.Models;

public interface IArgs { }
public record Opts(Dictionary<string, string> Args, Settings Settings);
public record Settings(Start Start, Timeouts Timeouts, Rando Rando, Rando Iterations);
public record Start(string Feature, string? Url, bool? New);
public record Timeouts(int Default, int Wait, int Navigate, Rando Naps);
public record Rando(int Min, int Max, int? Multiplier = null);

public interface IActor {
  // Input
  Opts Options { get; set; }

  // Run Environment
  IEnumerable<IScript> Scripts { get; set; }

  // Output
  // Storage
  // Integrations
}

// A Dictionary-based IArgs implementation that serializes properly
public class DictionaryArgs : Dictionary<string, object>, IArgs {
  public DictionaryArgs() : base() { }
  public DictionaryArgs(IDictionary<string, object> dictionary) : base(dictionary) { }
}
