using System.Collections.ObjectModel;
using Chameleon.AIR.Scripts.Models;
using Chameleon.AIR.Scripts.Reddit.Post;
using Chameleon.AIR.Scripts.Reddit.Subreddit;

namespace Chameleon.AIR.Actors.Models.Reddit;

public class Actor : IActor {
  public Opts Options { get; set; } = new Opts(
    //new Args("Search Term", Scope.Posts, Sort.Relevance, Filter.All),
    new () {
      { "Search", string.Empty },
      { "Scope", Scope.Posts.ToString() },
      { "Sort", Sort.Relevance.ToString() },
      { "Filter", Filter.All.ToString() }
    },
    new Settings(
      Start: new Start(
        Feature: "Reddit",
        Attempts: 9,
        Variations: new Rando(1, 3),
        Iterations: new Rando(3, 6),
        Rando: new Rando(6, 9)
      ),
      Timeouts: new Timeouts(30, 15, 60, new Rando(256, 512, null))
    )
  );
  public IEnumerable<IScript> Scripts { get; set; } = new ObservableCollection<IJSScript>() {
    new Comment(), new Reply(),
    new Join(), new Post(), new Vote(),
  };
}