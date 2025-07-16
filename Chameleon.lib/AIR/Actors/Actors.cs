using System.Collections.ObjectModel;
using Chameleon.lib.AIR.Scripts;
using Chameleon.lib.Util;

namespace Chameleon.lib.AIR.Actors;
// A generic dictionary-like interface
public interface IArti<T> {
  T this[string key] { get; set; }
}

// A concrete implementation for any type
public class Artifact : Dictionary<string, object>, IArti<object> {
  public new object this[string key] {
    get => ContainsKey(key) ? base[key] : default!;
    set => base[key] = value;
  }
}

public abstract record Args {
  public abstract Artifact ToDictionary(IEnumerable<Selection> selections);
  public abstract Args Set(Artifact sourceArgs);
}
public interface IActor {
  // Input
  Opts Options { get; set; }
  Args Args { get; set; }

  // Run Environment
  IEnumerable<IJSScript> Scripts { get; set; }

  // Output
  // Storage
  // Integrations
}

public record Decorations(string System, string Human, string Audience, string Background, string Tone);
public record AI(Decorations Decorators, string Model =  "o4-mini");
public record Rando(int Min, int Max, int? Multiplier = null);
public record Timeouts(int Default, int Wait, int Navigate, Rando Naps) {
  public Artifact Artifacto { get; set; } = new() { ["delay"] = 120 };
}
public record Start(string Feature, int Attempts, Rando Variations, Rando Iterations, bool New = true, string? Url = null, bool All = true) {
  public string Terms { get; set; } = string.Empty;
  public Rando Rando { get; set; } = new Rando(1, 1);
  public IEnumerable<string> Urls { get; set; } = [];
  public IEnumerable<string> Search { get; set; } = [];
}
public record Settings(Start Start, Timeouts Timeouts) {
  public bool EachProfile { get; set; }
  public bool CloseAfterRun { get; set; }
  public int Delay { get; set; } = 120;
  public int Variations => Start.Variations.Min;
  public Settings ToRecord(IEnumerable<string>? urls = null, IEnumerable<string>? search = null, Selection? selection = null, Rando? variations = null) {
    return this with {
      Start = Start with {
        Rando = selection?.Script.Title == "Surf" ? new(0, 0) : Start.Rando,
        Variations = variations ?? Start.Variations,
        Urls = urls ?? Start.Url?.Split('\n').Where(x => x.IsNot()).Select(x => x.Trim()) ?? [],
        Search = search ?? Start.Terms?.Split(',').Where(x => x.IsNot()).Select(x => x.Trim()) ?? []
      },
      Timeouts = Timeouts with {
        Artifacto = new() { ["delay"] = Delay }
      }
    };
  }
}
public record Opts(AI AI, Artifact Args, Settings Settings);
public record Selection(JSScript Script, bool Selected = false);
