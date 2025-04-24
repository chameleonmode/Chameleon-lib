using System.Collections.ObjectModel;
using Chameleon.AIR.Scripts.Models;
using Chameleon.AIR.Scripts.Reddit.Post;
using Chameleon.AIR.Scripts.Reddit.Subreddit;

namespace Chameleon.AIR.Actors.Models.Reddit;

public class Actor : IActor {
  public Opts<IArgs> Options { get; set; } = new Opts<IArgs>(
    new Args("Search Term", Scope.Posts, Sort.Relevance, Filter.All),
    new Settings(
      new Start("Reddit", "https://www.reddit.com", true),
      new Timeouts(36, 72, 18, new Rando(256, 512, null)),
      new Rando(18, 36),
      new Rando(1, 5)
    )
  );
  public IEnumerable<IScript> Scripts { get; set; } = new ObservableCollection<IJSScript>() {
    new Comment(), new Reply(),
    new Join(), new Post(), new Vote(),
  };
}