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
