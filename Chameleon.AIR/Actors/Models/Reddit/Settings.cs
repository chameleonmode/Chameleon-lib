namespace Chameleon.AIR.Actors.Models.Reddit;

public enum Scope {
  Posts,
  Communities,
  Comments,
  Media,
  People
}
public enum Sort {
  Relevance,
  Hot,
  Top,
  New,
  Rising,
  Comments
}
public enum Filter {
  All,
  Year,
  Month,
  Week,
  Today,
  Hour
}

public record Args(
  string Search,
  Scope Scope,
  Sort Sort,
  Filter Filter
) : IArgs;

public class Options : IOptions {
  public Opts<IArgs> Opts { get; set; } = new Opts<IArgs>(
    new Args("", Scope.Posts, Sort.Relevance, Filter.All),
    new Settings(
      new Start("Reddit", "https://www.reddit.com", true),
      new Timeouts(36, 72, 18, new Rando(256, 512, null)),
      new Rando(18, 36),
      new Rando(1, 5)
    )
  );
}

