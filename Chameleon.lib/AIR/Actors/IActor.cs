using Chameleon.lib.AIR.Scripts;

namespace Chameleon.lib.AIR.Actors;
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

public record Decorations(string System, string Human, string Audience, string Background, string Tone);
public record AI(Decorations Decorators);
public record Rando(int Min, int Max, int? Multiplier = null);
public record Timeouts(int Default, int Wait, int Navigate, Rando Naps) {
  public Artifact Artifacto { get; set; } = new() { ["delay"] = 120 };
}
public record Start(string Feature, int Attempts,Rando Variations, Rando Iterations, bool New = true, string? Url = null, bool All = true) {
  public Rando Rando { get; set; } = new Rando(1, 1);
  public IEnumerable<string>? Urls { get; set; }
}
public record Settings(Start Start, Timeouts Timeouts)
{
  public bool EachProfile { get; set; }
  public bool CloseAfterRun { get; set; }
  public int Delay { get; set; } = 120;
  	public Settings ToRecord(IEnumerable<string>? urls = null, Rando? rando = null, Rando? variations = null) {
		return new(
			Start with {
				Rando = rando ?? Start.Rando,
				Variations = variations ?? Start.Variations,
				Urls = urls ?? Start.Url?.Split('\n').Select(x => x.Trim())
			},
			 Timeouts with {
				Artifacto = new() { ["delay"] =  Delay }
			}
		);
	}
}
public record Opts(AI AI, DictionaryArgs Args, Settings Settings);

// A Dictionary-based IArgs implementation that serializes properly
public class DictionaryArgs : Dictionary<string, object>, IArgs
{
  public DictionaryArgs() : base() { }
  public DictionaryArgs(IDictionary<string, object> dictionary) : base(dictionary) { }
}

public record Selection(Script Script, bool Selected = false);
