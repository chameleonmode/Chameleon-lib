namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Subreddit;

public class Vote : JSScript {
	public Vote() : base(
		"reddit/plugins/subreddit/vote",
		"Reddit Vote",
		"Search for subreddit and rando vote up/down"
	) { }
}