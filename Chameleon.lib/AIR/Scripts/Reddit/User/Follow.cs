namespace Chameleon.lib.AIR.Scripts.Reddit.User;

public record Follow : JSScript {
	public Follow() : base(
		"../scripts/reddit/plugins/user/addons/follow",
		"Follow",
		"Follow a user and see their posts in your feed"
	) { }
}
