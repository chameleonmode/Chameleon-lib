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

// A Dictionary-based IArgs implementation that serializes properly
public class DictionaryArgs : Dictionary<string, object>, IArgs {
  public DictionaryArgs() : base() { }
  public DictionaryArgs(IDictionary<string, object> dictionary) : base(dictionary) { }
}
