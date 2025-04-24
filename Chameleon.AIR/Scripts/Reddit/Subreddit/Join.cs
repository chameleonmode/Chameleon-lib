using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Subreddit;

public class Join : JSScript {
	public Join() : base(
		"reddit/plugins/subreddit/join",
		"Reddit Join",
		"Search for reddit post finds subreddit and joins"
	) { }
}