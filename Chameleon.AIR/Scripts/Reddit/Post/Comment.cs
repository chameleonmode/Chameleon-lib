using Chameleon.AIR.Scripts.Models;

namespace Chameleon.AIR.Scripts.Reddit.Post;

public record Comment : JSScript {
	public Comment() : base(
		"reddit/plugins/post/comment",
		"Comment",
		"Search for post and comment on it"
	) { }
}