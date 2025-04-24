using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Subreddit;

public class Post : JSScript {
	public Post() : base(
		"reddit/plugins/subreddit/post",
		"Reddit Agent Post",
		"Search for relevant content and learn & post on a relevant subreddit"
	) { }
}