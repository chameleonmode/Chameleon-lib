using Chameleon.lib.Common.Models.Dto;

using DynamicData;

namespace Chameleon.lib.Api.Repos;

public static class Consts {
	public static class Api {
		public const string ApiBaseUrl = "https://api.chameleonmode.com/api/";
		public const string ServicesPath = "services/app/";
		public static class Endpoints {
			public const string Profile = $"{ServicesPath}profile/";
			public const string Folder = $"{ServicesPath}folder/";
			public const string Person = $"{ServicesPath}person/";
			public const string Business = $"{ServicesPath}business/";
			public const string Address = $"{ServicesPath}address/";
			public const string Credentia = $"{ServicesPath}credential/";
			public const string Country = $"{ServicesPath}country/";
			public const string Proxy = $"{ServicesPath}proxy/";
			public const string ProxyCredit = $"{ServicesPath}proxycredit/";
			public const string AssistantUser = $"{ServicesPath}assistantuser/";
			public const string ShareFolders = $"{ServicesPath}sharefolders/";
			public const string BrowserSettings = $"{ServicesPath}userdefaultsettings/";
		}
	}
}
public abstract class ApiBase<TDto> where TDto : IDto {
		public string Endpoint { get; }

		public IObservableCache<TDto, int> ObservableCache { get; }
		public SourceCache<TDto, int> SourceCache { get; }

		public ApiBase(string endpoint) {
			Endpoint = endpoint;
			SourceCache = new SourceCache<TDto, int>(p => p.id);
			ObservableCache = SourceCache.AsObservableCache();
		}

		private async Task<TDto> UpdateWithCache(Func<Task<TDto>> @dto) {
			var o = await @dto();
			SourceCache.AddOrUpdate(o);
			return o;
		}

		public virtual async Task Load() {
			var response = await GetAll<TDto>();
			SourceCache.Edit(innerCache => {
				innerCache.Clear();
				innerCache.AddOrUpdate(response);
			});
		}

		public virtual async Task<T[]> GetAll<T>() {
			var r = await HttpApiClient.Instance.Get<Result<T>>($"{Endpoint}GetAll?MaxResultCount={int.MaxValue}", new {
				MaxResultCount = int.MaxValue
			});
			ArgumentNullException.ThrowIfNull(r, "Response is unreadable");
			r.items ??= [];
			return r.items;
		}

		public virtual Task<TDto> Create(object dto) => UpdateWithCache(() => HttpApiClient.Instance.Post<TDto>($"{Endpoint}Create", dto));
		public virtual Task<TDto> Put(object dto) => UpdateWithCache(() => HttpApiClient.Instance.Put<TDto>($"{Endpoint}Update", dto));
		public virtual Task<TDto> Initialize(object dto) => UpdateWithCache(() => Task.FromResult((TDto)dto));
		public virtual async Task<RootResult> Delete(int id) {
			var o = await HttpApiClient.Instance.Delete<RootResult>($"{Endpoint}Delete?Id={id}");
			if (o.success) SourceCache.Remove(id);
			return o;
		}

		public virtual Task<RootResult> DeleteFromCache(TDto item) {
			if (SourceCache.Items.Any(x => x.id == item.id)) {
				SourceCache.Remove(item.id);
				return Task.FromResult(new RootResult() { success = true });
			}
			return Task.FromResult(new RootResult() { success = false });
		}

		public virtual Task<T> Get<T>(string path, object? body = default) {
			return HttpApiClient.Instance.Get<T>($"{Endpoint}{path}", body);
		}
		public virtual Task<T> Get<T>(int id) {
			return HttpApiClient.Instance.Get<T>($"{Endpoint}Get?Id={id}");
		}
		protected virtual Task<RootResult> Post(string path, object? dto = default) {
			return HttpApiClient.Instance.Post<RootResult>($"{Endpoint}{path}", dto);
		}
		protected virtual Task<T> Post<T>(string path, object? dto = default) {
			return HttpApiClient.Instance.Post<T>($"{Endpoint}{path}", dto);
		}
	}