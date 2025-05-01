using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Actors.Models;

public interface IArgs { }
public interface IActor
{
  // Input
  Opts Options { get; set; }

  // Run Environment
  IEnumerable<IScript> Scripts { get; set; }

  // Output
  // Storage
  // Integrations
}

public record Opts(DictionaryArgs Args, Settings Settings);
public record Settings(Start Start, Timeouts Timeouts);
public record Start(string Feature, int Attempts, Rando Variations, Rando Iterations, Rando Rando, bool New = true, string? Url = null, bool All = false) {
  public IEnumerable<string>? Urls { get; set; } = Url?.Split('\n').Select(x => x.Trim());
}
public record Timeouts(int Default, int Wait, int Navigate, Rando Naps);
public record Rando(int Min, int Max, int? Multiplier = null);


// A Dictionary-based IArgs implementation that serializes properly
public class DictionaryArgs : Dictionary<string, object>, IArgs {
  public DictionaryArgs() : base() { }
  public DictionaryArgs(IDictionary<string, object> dictionary) : base(dictionary) { }
}
