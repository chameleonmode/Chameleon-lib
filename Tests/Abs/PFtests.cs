using System.Diagnostics;
using Chameleon.lib;
using Chameleon.lib.Abs;
using Chameleon.lib.Util;
namespace Tests.Abs;

public record Proxzy(string? Host = null, int Port = 0, string? UserName = null, string? Password = null);
# region dtos
public interface IID {
  int Id { get; init; }
}
public interface IT {
  string TenantId { get; init; }
}
public interface IDT : IT {
  int? Id { get; init; }
  string? Jzon { get; set; }
}
public record ID : IID {
  public int Id { get; init; }
}
public record I : IT {
  public required string TenantId { get; init; }
}
public record Tenant(string TenantId) : ID;
public record Permission(string Name, string Description) : ID;
public record UserPermission(int PermissionId, int UserId) : I;
public record UserAccess(int UserId, int ProfileId, int FolderId) : I;
public record DT<T> : I, IDT {
  public int? Id { get; init; }
  public string? Jzon { get; set; }
  public T? O {
    get => JSON.Parse<T>(Jzon);
    set => Jzon = JSON.Stringify(value);
  }
}

public record EE  {
  public required string Title { get; init; }
  public bool IsFavourite { get; set; } = false;
  public string? Notes { get; set; }
}
public record Folderee : EE;
public record Folder : DT<Folderee>;

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

public record Profilee : EE {
  public Proxzy Proxy { get; set; } = new();
}
public class Jzon<T> {
  private string? json;
  private T? collection;
  
  internal string? Jzons {
    get => json;
    set {
      json = value;
      collection = default; // Clear cache when JSON changes
    }
  }
  
  internal T? Collection {
    get => collection ??= JSON.Parse<T>(json);
    set {
      collection = value;
      json = JSON.Stringify(value);
    }
  }
}

public record Profile : DT<Profilee> {
  public int? FolderId { get; init; } = null;

  public Jzon<List<Addressesee>> Addresses { get; set; } = new();
  public string? Addressez { get => Addresses.Jzons; set => Addresses.Jzons = value; }

  public Jzon<List<Businessesee>> Businesses { get; set; } = new();
  public string? Businessez { get => Businesses.Jzons; set => Businesses.Jzons = value; }

  public Jzon<List<Loginsee>> Logins { get; set; } = new();
  public string? Loginz { get => Logins.Jzons; set => Logins.Jzons = value; }

  public Jzon<List<Personesee>> Persons { get; set; } = new();
  public string? Personz { get => Persons.Jzons; set => Persons.Jzons = value; }
}
#endregion

public class Table<T, TT>(string prefix) : Web where T : DT<TT> {
  public Request Req { get; } = new(prefix + '/', Authenticate: !Debugger.IsAttached);
  public DT<TT> DTO => new() { TenantId = PF.I.Tenant.TenantId };
  public async Task<IEnumerable<T>?> Get() => await Get<IEnumerable<T>>(Req);
  public async Task<IEnumerable<T>?> Get(string q) => await Get<IEnumerable<T>>(Req with { Q = q });
  public async Task<T?> Get(int id) => await Get<T>(Req with { Path = $"{Req.Path}{id}" });
  public Task<T?> Create(TT entitee) => Post<T>(Req with {
    Body =  DTO with {
      O = entitee
    }
  });
  public async Task<T?> Update(T dt) =>await Put<T>(Req with {
    Path = $"{Req.Path}{dt.Id}",
    Body = dt
  });
  
  public async Task<T?> Delete(int id) => await Delete<T>(Req with { Path = $"{Req.Path}{id}" });
}
public class PF {
  public Tenant Tenant { get; set; } = new("b6633ec1-138f-4ec6-b9d0-71b0660c0a44");
  public Table<DT<Folderee>, Folderee> Folders { get; } = new("folders");
  public Table<Profile, Profilee> Profiles { get; } = new("profiles");

  PF() { }
  public static PF I { get; } = new();
}

public class Tests : TestSetup {
  [Fact]
  public async Task CreateFolder_ShouldReturnValidFolder() {
    var result = await PF.I.Folders.Create(new() { Title = "Folder" });

    Assert.NotNull(result);
    Assert.Equal(PF.I.Tenant.TenantId, result.TenantId);
    Assert.NotNull(result.O);
    Assert.Equal("Folder", result.O?.Title);
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
  public async Task GetProfiles_ShouldReturnCollection() {
    var get = await PF.I.Profiles.Get(1);
    Assert.NotNull(get);

    var result = await PF.I.Profiles.Update(get with {
      Addresses = new() {
        Collection = [
        new() { Title = "Test Address 1", AddressLine1 = "123 Main St", City = "Test City", State = "TS", Zip = "12345" },
        new() { Title = "Test Address 2", AddressLine1 = "456 Elm St", City = "Another City", State = "AS", Zip = "67890" }
      ]
      },
      Businesses = new() {
        Collection = [
          new() { Title = "Test Business 1", CompanyName = "Test Company", PhoneNumber = "123-456-7890", WebSite = "https://testcompany.com" },
          new() { Title = "Test Business 2", CompanyName = "Another Company", PhoneNumber = "987-654-3210", WebSite = "https://anothercompany.com" }
        ]
      },
      Persons = new() {
        Collection = [
          new() { Title = "Test Person 1", FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
          new() { Title = "Test Person 2", FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" }
        ]
      },
      Logins = new() {
        Collection = [
          .. get.Logins.Collection ?? [], // Keep existing logins
          new() { Title = "Test Login 1", WebSite = "https://testlogin1.com", UserName = "user1", Password = "pass1" },
          new() { Title = "Test Login 2", WebSite = "https://testlogin2.com", UserName = "user2", Password = "pass2" }
        ]
      }
    });

    Assert.NotNull(result);
    Assert.NotNull(result.Addressez);
    Assert.NotNull(result.Businessez);
    Assert.NotNull(result.Personz);
    Assert.NotNull(result.Loginz);
  }
  [Fact]
  public async Task GetProfilesAddresses_ShouldReturnEmptyAddressCollection() {
    var get = await PF.I.Profiles.Get(1);
    Assert.NotNull(get);
    var result = await PF.I.Profiles.Update(
      get with {
        Addresses = new() {
          Collection = [] // Empty collection
        },
      }
    );

    Assert.NotNull(result);
    Assert.NotNull(result.Businessez);
    Assert.Empty(result.Addresses.Collection); // TODO
  }

  // [Fact]
  // public async Task GetProfilesBusinesses_ShouldReturnCollection() {
  //   var result = await PF.I.Profiles.DTO.Businesses.Get();

  //   Assert.NotNull(result);
  // }

  // [Fact]
  // public async Task GetProfilesAddresses_ShouldReturnCollection() {
  //   var result = await PF.I.Profiles.DTO.Addresses.Get();

  //   Assert.NotNull(result);
  // }

  // [Fact]
  // public async Task GetProfilesLogins_ShouldReturnCollection() {
  //   var result = await PF.I.Profiles.DTO.Logins.Get();

  //   Assert.NotNull(result);
  // }
}
