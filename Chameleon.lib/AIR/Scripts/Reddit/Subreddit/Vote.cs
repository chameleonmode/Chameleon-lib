namespace Chameleon.lib.AIR.Scripts.Reddit.Subreddit;

public record Vote : JSScript {
	public Vote() : base(
		"../scripts/reddit/plugins/subreddit/addons/vote",
		"Vote",
		"Search for subreddit and rando vote up/down"
	) { }
}

public record Surf : JSScript {
	public Surf() : base(
		"../scripts/reddit/plugins/subreddit/addons/vote",
		"Surf",
		"Just surf the subreddits and ride the wave of content"
	) { }
}