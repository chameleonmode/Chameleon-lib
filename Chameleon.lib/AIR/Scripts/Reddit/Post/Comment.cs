namespace Chameleon.lib.AIR.Scripts.Reddit.Post;

public record Comment : JSScript {
	public Comment() : base(
		"../scripts/reddit/plugins/post/comment",
		"Comment",
		"Search for post and comment on it"
	) { }
}