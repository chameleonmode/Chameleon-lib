using Chameleon.lib.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Post;

public record Reply : JSScript {
	public Reply() : base(
		"../scripts/reddit/plugins/post/reply",
		"Reply",
		"Search for a post to reply with context on a comment"
	) { }
}