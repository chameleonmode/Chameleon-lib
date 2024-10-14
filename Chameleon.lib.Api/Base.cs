using System.IO;

using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models.Interfaces;

using DynamicData;

namespace Chameleon.lib.Api;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
public class RootResponse<T> {
	public T? result { get; set; }
	public object? targetUrl { get; set; }
	public bool success { get; set; }
	public object? error { get; set; }
	public bool unAuthorizedRequest { get; set; }
	public bool __abp { get; set; }
}
public class RootResult : RootResponse<object> {
}

public class Result<T> {
	public int totalCount { get; set; }
	public T[]? items { get; set; }
}

public abstract class ApiBase<TDto>(string endpoint) where TDto : IHasid {
	public SourceCache<TDto, int> SourceCache { get; } = new(p => p.id);
	public IObservableCache<TDto, int> ObservableCache => SourceCache.AsObservableCache();

	private async Task<TDto> UpdateWithCache(Func<Task<TDto>> @dto)
	{
		var o = await @dto();
		SourceCache.AddOrUpdate(o);
		return o;
	}

	public async Task Load()
	{
		var response = await GetAll<TDto>();
		SourceCache.Edit(innerCache => {
			innerCache.Clear();
			innerCache.AddOrUpdate(response);
		});
	}

	protected async Task<T[]> GetAll<T>()
	{
		var r = await HttpApiClient.Instance.Get<Result<T>>($"{endpoint}GetAll", new
		{
			MaxResultCount = int.MaxValue
		});
		ArgumentNullException.ThrowIfNull(r, "Response is unreadable");
		r.items ??= [];
		return r.items;
	}

	protected virtual Task<TDto> Create(object dto)
	{
		return UpdateWithCache(()=> HttpApiClient.Instance.Post<TDto>($"{endpoint}Create", dto));
	}
	protected virtual Task<TDto> Save(TDto dto)
	{
		return UpdateWithCache(() => SourceCache.Items.Any(i => i.id == dto.id) ? Put(dto) : Create(dto));
	}
	protected virtual Task<TDto> Put(object dto)
	{
		return UpdateWithCache(() => HttpApiClient.Instance.Put<TDto>($"{endpoint}Update", dto));
	}

	protected virtual Task<T> Get<T>(string path)
	{
		return HttpApiClient.Instance.Get<T>($"{endpoint}{path}");
	}
	protected virtual Task<T> Get<T>(int id)
	{
		return HttpApiClient.Instance.Get<T>($"{endpoint}Get?Id={id}");
	}
	protected virtual Task<RootResult> Post(string path, object dto)
	{
		return HttpApiClient.Instance.Post($"{endpoint}{path}", dto);
	}
	public virtual async Task<RootResult> Delete(int id)
	{
		var o = await HttpApiClient.Instance.Delete($"{endpoint}Delete?Id={id}");
		if(o.success) {
			SourceCache.Remove(id);
		}
		return o;
	}
}