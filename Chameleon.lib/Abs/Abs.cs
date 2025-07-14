using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Chameleon.lib.Auth;
using Chameleon.lib.Util;

namespace Chameleon.lib.Abs;

#region Models / Dto's
public interface IID {
  int? Id { get; init; }
}
public interface IDT : IID {
  public string TenantId { get; init; }
}
public record Tag(int Id, string Name, string Items, string TenantId);
public record ItemTag(string TagItemType, string TagItemId, string TagName, string TenantId);

public record ID : IID {
  public int? Id { get; init; }
}
public record Tenant(string TenantId) : ID;
public record Permission(string Name, string Description) : ID;

/// Platformatic {\"statusCode\":400,\"code\":\"DB_USER\",\"error\":\"Bad Request\",\"message\":\"user exists\"}
public record Errorer(int? StatusCode, string? Code, string? Error, string? Message);
public record Request(string? Path = null,
  string? Q = null,
  object? Body = null,
  bool EnsureSuccess = true,
  bool Authenticate = true,
  HttpCompletionOption CompletionOption = HttpCompletionOption.ResponseContentRead,
  Dictionary<string, string>? Headers = null
) {
  public string Uri => $"{Path}{Q ?? ""}";
  public HttpContent? Content => Body == null ? null : JsonContent.Create(Body, mediaType: null, JSON.InsensitiveCamelCaseOptions);
}

public abstract class Root(string prefix) {
  public string Prefix { get; } = '/' + prefix;
}
public abstract class Web {
  static Task<T?> Sender<T>(HttpMethod method, Request request) => Abs.Send<T>(method, request);
  public static Task<T?> Put<T>(Request request) => Sender<T>(HttpMethod.Put, request);
  public static Task<T?> Post<T>(Request request) => Sender<T>(HttpMethod.Post, request);
  public static Task<T?> Get<T>(Request request) => Sender<T>(HttpMethod.Get, request);
  public static Task<T?> Delete<T>(Request request) => Sender<T>(HttpMethod.Delete, request);
}
public abstract class DTO<T>(string prefix) : Web where T : IDT {
  public Request Req { get; } = new(prefix + '/', Authenticate: !Debugger.IsAttached);
  
  public async Task<T?> Get(int? id) => await Get<T>(Req with { Path = $"{Req.Path}{id}" });
  public async Task<IEnumerable<T>?> Get() => await Get<IEnumerable<T>>(Req);
  public async Task<IEnumerable<T>?> Get(string q) => await Get<IEnumerable<T>>(Req with { Q = q });

  public Task<T?> Create(T dt) => Post<T>(Req with { Body = dt });

  public async Task<T?> Update(T dt) => await Put<T>(Req with { Path = $"{Req.Path}{dt.Id}", Body = dt });

  public async Task<T?> Delete(int? id) => await Delete<T>(Req with { Path = $"{Req.Path}{id}" });
  public async Task<T?> Delete(T dt) => await Delete<T>(Req with { Path = $"{Req.Path}{dt.Id}" });
}
#endregion

public static class Abs {
  private static Task<bool>? testing;
  public static Task<bool> TESTING => testing ??= Task.Run(async () => {
    try {
      using var client = new HttpClient();
      client.Timeout = TimeSpan.FromMilliseconds(300);
      _ = await client.GetAsync("http://127.0.0.1:3042");
      return true && Debugger.IsAttached; // Local server is available
    } catch {
      return false; // Use fallback
    }
  });

  public static async Task<HttpClient> HttpClient() => new HttpClient(new HttpClientHandler {
    AutomaticDecompression = DecompressionMethods.GZip,
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
  }) { BaseAddress = new Uri(
    await TESTING
       ? "http://127.0.0.1:3042" 
       : "https://chameleon-ws.onrender.com"
  )};

  public static async Task<T?> Send<T>(HttpMethod method, Request req) {
    using var client = await HttpClient();
    if (req.Authenticate) {
      var (auth0client, authentication) = await Session.I.Authenticate();
      client.DefaultRequestHeaders.Authorization = authentication;
      client.DefaultRequestHeaders.Add("x-auth0-identity", $"identity {auth0client.Token?.id_token}");
    }
    foreach (var header in req.Headers ?? []) client.DefaultRequestHeaders.Add(header.Key, header.Value);

    using var response = await client.SendAsync(new(method, req.Uri) { Content = req.Content }, req.CompletionOption);

    if (req.CompletionOption == HttpCompletionOption.ResponseHeadersRead) {
      _ = response.EnsureSuccessStatusCode();
      return typeof(T) == typeof(HttpResponseMessage) ? (T)(object)await response.Content.ReadAsStreamAsync() : default;
    }

    var content = await response.Content.ReadAsStringAsync();
    return response.IsSuccessStatusCode || !req.EnsureSuccess ? JSON.Deserialize<T>(content) 
    : JSON.Deserialize<Errorer>(content) is Errorer err ? throw new Exception($"{req.Uri}: {err.Error} {err.Message}") 
      : throw new HttpRequestException($"{req.Uri}: {response.StatusCode} {content}");
  }
}

public class Plt {
  public Tenant Tenant { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }//new("b6633ec1-138f-4ec6-b9d0-71b0660c0a44");

  public Plt() { }
  public static Plt I { get; } = new();
}

