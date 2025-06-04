using Chameleon.lib.AIR.Scripts.Models;

namespace Chameleon.AIR.Actors.Models;
// A generic dictionary-like interface
public interface IArti<T>
{
  T this[string key] { get; set; }
}

// A concrete implementation for any type
public class Artifact : Dictionary<string, object>, IArti<object>
{
  public new object this[string key]
  {
    get => ContainsKey(key) ? base[key] : default!;
    set => base[key] = value;
  }
}

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

public record Decorations(string System, string Prefix, string Human, string Audience, string Background, string Tone, string Suffix);
public record AI(Decorations Decorators);
public record Rando(int Min, int Max, int? Multiplier = null);
public record Timeouts(int Default, int Wait, int Navigate, Rando Naps);
public record Start(string Feature, int Attempts, Rando Variations, Rando Iterations, Rando Rando, bool New = true, string? Url = null, bool All = true) {
  public IEnumerable<string>? Urls { get; set; } = Url?.Split('\n').Select(x => x.Trim());
	public bool CloseOldBrowserProfileAfterRun { get; set; } = false;
	public bool ExecuteOneScriptAccrosProfiles { get; set; } = false;
}
public record Settings(Start Start, Timeouts Timeouts);
public record Opts(AI AI, DictionaryArgs Args, Settings Settings);

// A Dictionary-based IArgs implementation that serializes properly
public class DictionaryArgs : Dictionary<string, object>, IArgs {
  public DictionaryArgs() : base() { }
  public DictionaryArgs(IDictionary<string, object> dictionary) : base(dictionary) { }
}
