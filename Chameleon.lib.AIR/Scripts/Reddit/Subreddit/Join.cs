using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Subreddit;

public record Join : JSScript {
	public Join() : base(
		"../scripts/reddit/plugins/subreddit/join",
		"Join",
		"Search for reddit post finds subreddit and joins"
	) { }
}