using System.Diagnostics;
using Chameleon.lib;
using Chameleon.lib.Abs;
using Chameleon.lib.Abs.Platformatic;
namespace Tests.Abs;

public interface IID {
  int? Id { get; init; }
}
public record J2on<T> {
  public string Jzon { get; set; } = string.Empty;
  public T O {
    get => JSON.Parse<T>(Jzon);
    set => Jzon = JSON.Stringify(value);
  }
}

public record EE {
  public required string Title { get; init; }
  public bool IsFavourite { get; set; } = false;
  public string? Notes { get; set; }
}
public record Folderee : EE;
public record Profilee : EE {
  public Proxzy Proxy { get; set; } = new();
  public record Proxzy(string? Host, int Port, string? UserName, string? Password) {
    public Proxzy() : this(null, 0, null, null) { }
    public Proxzy(string? host) : this(host, 0, null, null) { }
    public Proxzy(string? host, int port) : this(host, port, null, null) { }
  };
}
public record Addressesee : EE {
  public int? CountryId { get; set; }
  public string? AddressLine1 { get; set; }
  public string? AddressLine2 { get; set; }
  public string? City { get; set; }
  public string? State { get; set; }
  public string? Zip { get; set; }
}
public record Businessesee : EE {
  public string? CompanyName { get; set; }
  public string? Department { get; set; }
  public string? PhoneNumber { get; set; }
  public string? WebSite { get; set; }
}
public record Loginsee : EE {
  public string? WebSite { get; set; }
  public string? Email { get; set; }
  public string? UserName { get; set; }
  public string? Password { get; set; }
}
public record Personesee : EE {
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public string? MiddleName { get; set; }
  public string? JobTitle { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public DateTime BirthDate { get; set; } = DateTimeOffset.Now.AddYears(-20).DateTime;
  public DateTimeOffset BirthDateOffset => new(BirthDate);
}

# region dtos
public record ID : IID {
  public int? Id { get; init; }
}
public record Tenant(string TenantId) : ID;
public record Permission(string Name, string Description) : ID;

public record DT<T> : J2on<T>, IID {
  public int? Id { get; init; }
  public required string TenantId { get; init; }
}
public record Folder : DT<Folderee>;
public record Profile : DT<Profilee> {
  public int? FolderId { get; init; } = null;
}

public record Identity<T> : DT<List<T>> {
  public required int ProfileId { get; init; }
}
public record Addressez : Identity<Addressesee>;
public record Businessez : Identity<Businessesee>;
public record Personz : Identity<Personesee>;
public record Loginz : Identity<Loginsee>;
#endregion

public class Table<T, TT>(string prefix) : Web where T : DT<TT> {
  public Request Req { get; } = new(prefix + '/', Authenticate: !Debugger.IsAttached);

  public virtual DT<TT> DTO => new() { TenantId = PF.I.Tenant.TenantId };

  public async Task<T?> Get(int? id) => await Get<T>(Req with { Path = $"{Req.Path}{id}" });
  public async Task<IEnumerable<T>?> Get() => await Get<IEnumerable<T>>(Req);
  public async Task<IEnumerable<T>?> Get(string q) => await Get<IEnumerable<T>>(Req with { Q = q });

  public Task<T?> Create(T dt) => Post<T>(Req with { Body = dt });
  public Task<T?> Create(TT entitee) => Post<T>(Req with { Body = DTO with { O = entitee } });

  public async Task<T?> Update(T dt) => await Put<T>(Req with { Path = $"{Req.Path}{dt.Id}", Body = dt });

  public async Task<T?> Delete(int? id) => await Delete<T>(Req with { Path = $"{Req.Path}{id}" });
  public async Task<T?> Delete(T dt) => await Delete<T>(Req with { Path = $"{Req.Path}{dt.Id}" });
}
public class PF {
  public Tenant Tenant { get; set; } = new(DB.Instance.DBuser?.TenantId ?? "b6633ec1-138f-4ec6-b9d0-71b0660c0a44");
  public Table<Folder, Folderee> Folders { get; } = new("folders");
  public Table<Profile, Profilee> Profiles { get; } = new("profiles");
  public Table<Addressez, List<Addressesee>> Addressez { get; } = new("addressez");
  public Table<Businessez, List<Businessesee>> Businessez { get; } = new("businessez");
  public Table<Personz, List<Personesee>> Personz { get; } = new("personz");
  public Table<Loginz, List<Loginsee>> Loginz { get; } = new("loginz");

  PF() { }
  public static PF I { get; } = new();
}

public class Tests : TestSetup {
  #region  Folders
  [Fact]
  public async Task CreateFolder_ShouldReturnValidFolder() {
    var result = await PF.I.Folders.Create(new Folderee() { Title = "Folder" });

    Assert.NotNull(result);
    Assert.Equal(PF.I.Tenant.TenantId, result.TenantId);
    Assert.NotNull(result.O);
    Assert.Equal("Folder", result.O.Title);
  }

  [Fact]
  public async Task GetFolders_ShouldReturnFolderCollection() {
    var result = await PF.I.Folders.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task UpdateFolder_ShouldReturnUpdatedFolder() {
    var folder = new Folder {
      Id = 0,
      TenantId = PF.I.Tenant.TenantId,
      O = new Folderee { Title = "Updated Folder", IsFavourite = true }
    };

    var result = await PF.I.Folders.Update(folder);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task DeleteFolder_ShouldReturnDeletedFolder() {
    var result = await PF.I.Folders.Delete(0);

    Assert.NotNull(result);
  }

  [Fact]
  public void Folder_JsonSerialization_ShouldWorkCorrectly() {
    var folderData = new Folderee { Title = "Test", IsFavourite = true };
    var folder = new Folder {
      TenantId = PF.I.Tenant.TenantId,
      O = folderData
    };

    Assert.Equal("Test", folder.O?.Title);
    Assert.True(folder.O?.IsFavourite);
    Assert.NotNull(folder.Jzon);
  }

  [Fact]
  public void Folder_EmptyJson_ShouldReturnDefault() {
    var folder = new Folder {
      TenantId = PF.I.Tenant.TenantId,
    };

    Assert.Null(folder.O);
  }

  #endregion

  // .........
  // Profile tests
  // .........

  [Fact]
  public async Task CreateProfile_ShouldReturnValidProfile() {
    var profilee = new Profilee { Title = "Test Profile" };
    var profile = new Profile {
      TenantId = PF.I.Tenant.TenantId,
      FolderId = null,
      O = profilee,
    };
    var result = await PF.I.Profiles.Create(profilee);

    Assert.NotNull(result);
    Assert.Equal(PF.I.Tenant.TenantId, result.TenantId);
    Assert.NotNull(result.O);
    Assert.Equal(profilee.Title, result.O.Title);
    Assert.Equal(profile.FolderId, result.FolderId);
  }

  [Fact]
  public async Task GetProfiles_ShouldReturnProfileCollection() {
    var result = await PF.I.Profiles.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task UpdateProfile_ShouldReturnUpdatedProfile() {
    var profile = new Profile {
      Id = 1,
      TenantId = PF.I.Tenant.TenantId,
      O = new Profilee { Title = "Updated Profile" }
    };

    var result = await PF.I.Profiles.Update(profile);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task DeleteProfile_ShouldReturnDeletedProfile() {
    var result = await PF.I.Profiles.Delete(1);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task CreateAddressez_ShouldReturnValidAddressez() {
    var created = await PF.I.Addressez.Create(new Addressez {
      TenantId = PF.I.Tenant.TenantId,
      ProfileId = 1,
      O = [new Addressesee { Title = "Test Address", AddressLine1 = "123 Main St", City = "Test City", State = "TS", Zip = "12345" }]
    });
    Assert.NotNull(created);

    var get = await PF.I.Addressez.Get(created.Id);
    Assert.NotNull(get);
  }

  [Fact]
  public async Task CreateBusinessez_ShouldReturnValidBusinessez() {
    var b = await PF.I.Businessez.Create(new Businessez {
      TenantId = PF.I.Tenant.TenantId,
      ProfileId = 1,
      O = [new Businessesee { Title = "Test Business", CompanyName = "Test Company", PhoneNumber = "123-456-7890", WebSite = "https://testcompany.com" }]
    });
    Assert.NotNull(b);
  }

  [Fact]
  public async Task CreatePersonz_ShouldReturnValidPersonz() {
    var p = await PF.I.Personz.Create(new Personz {
      TenantId = PF.I.Tenant.TenantId,
      ProfileId = 1,
      O = [new Personesee { Title = "Test Person", FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" }]
    });
    Assert.NotNull(p);
  }

  [Fact]
  public async Task CreateLoginz_ShouldReturnValidLoginz() {
    var l = await PF.I.Loginz.Create(new Loginz {
      TenantId = PF.I.Tenant.TenantId,
      ProfileId = 1,
      O = [new Loginsee { Title = "Test Login", WebSite = "https://testlogin.com", UserName = "user", Password = "pass" }]
    });
    Assert.NotNull(l);
  }
}
