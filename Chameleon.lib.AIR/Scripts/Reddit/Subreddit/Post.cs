using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Subreddit;

public record Post : JSScript {
	public Post() : base(
		"../scripts/reddit/plugins/subreddit/post",
		"Post",
		"Search for relevant content and learn & post on a relevant subreddit"
	) { }
}