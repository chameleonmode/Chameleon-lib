using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Actors.Models;

public interface IArgs { }
public interface IOptions {
  Opts<IArgs> Opts { get; set; }
}
public record Opts<T>(T Args, Settings Settings);
public record Settings(Start Start, Timeouts Timeouts, Rando Rando, Rando Iterations);
public record Start(string Feature, string? Url, bool? New);
public record Timeouts(int Default, int Wait, int Navigate, Rando Naps);
public record Rando(int Min, int Max, int? Multiplier = null);

public interface IActor {
  //Input
  IOptions Options { get; set; }

  //Run Environment
  IEnumerable<IScript> Scripts { get; set; }

  //Output
  //Storage
  //Integrations
}
