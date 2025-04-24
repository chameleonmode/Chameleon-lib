using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Post;

public class Reply : JSScript {
	public Reply() : base(
		"reddit/plugins/post/reply",
		"Reply",
		"Search for a post to reply with context on a comment"
	) { }
}