using System.Collections.ObjectModel;
using Chameleon.AIR.Scripts.Models;
using Chameleon.AIR.Scripts.Reddit.Post;
using Chameleon.AIR.Scripts.Reddit.Subreddit;

namespace Chameleon.AIR.Actors.Models.Reddit;

public class Actor : IActor {
	public IOptions Options { get; set; } = new Options();
	public IEnumerable<IScript> Scripts { get; set; } = new ObservableCollection<IJSScript>() {
    new Comment(),
		new Reply(),
		new Join(),
		new Post(),
		new Vote(),
  };
}