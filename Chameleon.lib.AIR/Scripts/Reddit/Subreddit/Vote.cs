using Chameleon.lib.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Subreddit;

public record Vote : JSScript {
	public Vote() : base(
		"../scripts/reddit/plugins/subreddit/vote",
		"Vote",
		"Search for subreddit and rando vote up/down"
	) { }
}