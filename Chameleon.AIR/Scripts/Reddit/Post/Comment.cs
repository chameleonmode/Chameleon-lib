using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Post;

public class Comment : JSScript {
	public Comment() : base(
		"reddit/plugins/post/comment",
		"Comment On Post",
		"Search for post and comment on it"
	) { }
}