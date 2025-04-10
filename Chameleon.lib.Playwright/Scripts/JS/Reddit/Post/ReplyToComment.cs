namespace Chameleon.lib.Playwright.Scripts.JS.Reddit.Post;

public class ReplyToComment : JSScript {
	public ReplyToComment() : base(
		"reddit/plugins/post/reply-to-comment",
		"Reddit Reply To A Post Comment",
		"Search for a reddit post reply with context to a comment"
	) { }
}