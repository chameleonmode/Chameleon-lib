using Chameleon.lib.Api.Repos;

//using DynamicData;

namespace Tests.APiv1;
public class UserAssistantRepoTests {
	readonly UserAssistantRepo userAssistantRepo = UserAssistantRepo.Instance;
	readonly ShareFoldersRepo shareFoldersRepo = ShareFoldersRepo.Instance;
	[Fact]
	public async Task TestShareFoldersRepo() {
		_ = userAssistantRepo.ObservableCache.Connect().Subscribe(Console.WriteLine);
		await userAssistantRepo.Load();
		foreach (var item in userAssistantRepo.ObservableCache.Items) {
			Console.WriteLine(item);
			_ = await ShareFoldersRepo.GetAll(item.id);
		}
	}
}
