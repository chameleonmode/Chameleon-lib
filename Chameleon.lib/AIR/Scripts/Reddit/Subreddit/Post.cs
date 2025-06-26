namespace Chameleon.lib.AIR.Scripts.Reddit.Subreddit;

public record Post : JSScript {
	public Post() : base(
		"../scripts/reddit/plugins/subreddit/addons/post",
		"Post",
		"Search for relevant content and learn & post on a relevant subreddit"
	) { }
}