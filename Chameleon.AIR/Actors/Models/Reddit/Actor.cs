using System.Collections.ObjectModel;
using Chameleon.AIR.Scripts.Models;
using Chameleon.AIR.Scripts.Reddit.Post;
using Chameleon.AIR.Scripts.Reddit.Subreddit;

namespace Chameleon.AIR.Actors.Models.Reddit;

public class Actor : IActor {
  public Opts Options { get; set; } = new Opts(
    //new Args("Search Term", Scope.Posts, Sort.Relevance, Filter.All),
    new Dictionary<string, string>() {
      { "Search", "Search Term" },
      { "Scope", Scope.Posts.ToString() },
      { "Sort", Sort.Relevance.ToString() },
      { "Filter", Filter.All.ToString() }
    },
    new Settings(
      new Start("Reddit", "https://www.reddit.com", true),
      new Timeouts(60, 30, 120, new Rando(256, 512, null)),
      new Rando(9, 18),
      new Rando(1, 3)
    )
  );
  public IEnumerable<IScript> Scripts { get; set; } = new ObservableCollection<IJSScript>() {
    new Comment(), new Reply(),
    new Join(), new Post(), new Vote(),
  };
}