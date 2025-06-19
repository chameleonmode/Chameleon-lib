using Chameleon.lib;
using Chameleon.lib.Abs;

namespace Tests.Abs;

public interface II {
  string TenantId { get; init; }
  int TenintId { get; init; }
}
public interface IID {
  int Id { get; init; }
}
public interface IDT : II, IID {
  string? Jzon { get; set; }
}
public record ID : IID {
  public int Id { get; init; }
}
public record Tenant(string TenantId) : ID;
public record Permission(string Name, string Description) : ID;
public record I : II {
  public required string TenantId { get; init; }
  public required int TenintId { get; init; }
}
public record UserPermission(int PermissionId, int UserId) : I;
public record UserAccess(int UserId, int ProfileId, int FolderId) : I;
public record FolderProfile(int FolderId, int ProfileId) : I;

public record DT<T> : I, IDT {
  public int Id { get; init; }
  public string? Jzon { get; set; }
  public T? O {
    get => Jzon == null ? default : JSON.Deserialize<T>(Jzon);
    set => Jzon = value != null ? JSON.Serialize(value) : string.Empty;
  }
}

public record EE {
  public required string Title { get; init; }
  public bool IsFavourite { get; set; } = false;
  public string? Notes { get; set; }
}

public record Folder : DT<Folderee>;
public record Folderee(int ProfilesCount = 0) : EE;

public record Profile : DT<Profilee>;
public record Profilee(int FolderId = -1) : EE {
  public record Proxzy(string? Host = null, int Port = 0, string? UserName = null, string? Password = null);
  public Proxzy Proxy { get; set; } = new();
}

public record Identitee : EE {
  public required int ProfileId { get; init; }
}
public record ProfilePersons : DT<Personesee>;
public record Personesee : Identitee {
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public string? MiddleName { get; set; }
  public string? JobTitle { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public string? BirthPlace { get; set; }
  public DateTime BirthDate { get; set; } = DateTimeOffset.Now.AddYears(-20).DateTime;
  public DateTimeOffset BirthDateOffset => new(BirthDate);
}

public record ProfileBusinesses : DT<Businessesee>;
public record Businessesee : Identitee {
  public string? CompanyName { get; set; }
  public string? Department { get; set; }
  public string? PhoneNumber { get; set; }
  public string? WebSite { get; set; }
}

public record ProfileAddresses : DT<Addressesee>;
public record Addressesee : Identitee {
  public int? CountryId { get; set; }
  public string? AddressLine1 { get; set; }
  public string? AddressLine2 { get; set; }
  public string? City { get; set; }
  public string? State { get; set; }
  public string? Zip { get; set; }
}

public record ProfileLogins : DT<Loginsee>;
public record Loginsee : Identitee {
  public string? WebSite { get; set; }
  public string? Email { get; set; }
  public string? UserName { get; set; }
  public string? Password { get; set; }
}
public abstract class Repo<T>(string prefix) : Web {
  public string Prefix { get; } = '/' + prefix + '/';
  public async Task<IEnumerable<T>?> Get() => await Get<IEnumerable<T>?>(Prefix);
}
public abstract class Table<T, TT>(string prefix) : Repo<T>(prefix) where T : IDT {
  public Task<T?> Create(TT entitee) => Post<T>(Prefix, new Request(
    Body: new DT<TT> {
      TenantId = PF.Instance.Tenant!.TenantId,
      TenintId = PF.Instance.Tenant.Id,
      O = entitee
    }
  ));
  public async Task<T?> Update(T dt) => await Put<T?>($"{Prefix}{dt.Id}", new(Body: dt));
  public async Task<T?> Delete(int id) => await Delete<T?>($"{Prefix}{id}");
}
public abstract class JunctionTable<T, TT, TTT>(string prefix): Repo<T>(prefix) where T : II where TT : IDT where TTT : IDT {
  public async Task<T?> Create(Request request) => await Post<T>(Prefix, request);
  public async Task<T?> Update(Request request) => await Put<T?>($"{Prefix}", request);
  public async Task<T?> Delete(Request request) => await Delete<T?>($"{Prefix}", request);
}

public class PF {
  public Tenant? Tenant { get; set; }
  public static class Tables {
    public class FoldersProfiles : JunctionTable<FolderProfile, DT<Folderee>, DT<Profilee>> {
      FoldersProfiles() : base("folderProfiles") { }
      public async Task<FolderProfile?> Create(int folderId, int profileId) {
        var datas = await Create(new Request(
          Body: new FolderProfile(folderId, profileId) {
            TenantId = PF.Instance.Tenant!.TenantId,
            TenintId = PF.Instance.Tenant.Id
          }
        ));
        return datas;
      }
      public async Task<FolderProfile?> Update(FolderProfile folderProfile) {
        var datas = await Update(new Request(
          Body: folderProfile
        ));
        return datas;
      }
      public static FoldersProfiles Instance { get; } = new();
    }
    public class Folders : Table<Folder, Folderee> {
      Folders() : base("folders") { }
      public static Folders Instance { get; } = new();
    }
    public class Profiles : Table<Profile, Profilee> {
      Profiles() : base("profiles") { }
      public static Profiles Instance { get; } = new();
    }
    public class ProfilesPersons : Table<ProfilePersons, Personesee> {
      ProfilesPersons() : base("profilePersons") { }
      public static ProfilesPersons Instance { get; } = new();
    }
    public class ProfilesBusinesses : Table<ProfileBusinesses, Businessesee> {
      ProfilesBusinesses() : base("profileBusinesses") { }
      public static ProfilesBusinesses Instance { get; } = new();
    }
    public class ProfilesAddresses : Table<ProfileAddresses, Addressesee> {
      ProfilesAddresses() : base("profileAddresses") { }
      public static ProfilesAddresses Instance { get; } = new();
    }
    public class ProfilesLogins : Table<ProfileLogins, Loginsee> {
      ProfilesLogins() : base("profileLogins") { }
      public static ProfilesLogins Instance { get; } = new();
    }
  }

  PF() { }
  public static PF Instance { get; } = new();
}

public class Tests : TestSetup {
  public string UserId { get; } = "b6633ec1-138f-4ec6-b9d0-71b0660c0a41";
  public Tenant Tenant { get; } = new("b6633ec1-138f-4ec6-b9d0-71b0660c0a42") { Id = 1 };
  public Tests() {
    PF.Instance.Tenant = Tenant;
  }
  [Fact]
  public async Task CreateFolder_ShouldReturnValidFolder() {
    var folderData = new Folderee { Title = "Test Folder" };
    var result = await PF.Tables.Folders.Instance.Create(folderData);

    Assert.NotNull(result);
    Assert.Equal(Tenant.TenantId, result.TenantId);
    Assert.Equal(Tenant.Id, result.TenintId);
    Assert.NotNull(result.O);
    Assert.Equal("Test Folder", result.O.Title);
  }

  [Fact]
  public async Task GetFolders_ShouldReturnFolderCollection() {
    var result = await PF.Tables.Folders.Instance.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task UpdateFolder_ShouldReturnUpdatedFolder() {
    var folder = new Folder {
      Id = 1,
      TenantId = Tenant.TenantId,
      TenintId = Tenant.Id,
      O = new Folderee { Title = "Updated Folder", IsFavourite = true }
    };

    var result = await PF.Tables.Folders.Instance.Update(folder);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task DeleteFolder_ShouldReturnDeletedFolder() {
    var result = await PF.Tables.Folders.Instance.Delete(1);

    Assert.NotNull(result);
  }

  [Fact]
  public void Folder_JsonSerialization_ShouldWorkCorrectly() {
    var folderData = new Folderee { Title = "Test", ProfilesCount = 5, IsFavourite = true };
    var folder = new Folder {
      TenantId = Tenant.TenantId,
      TenintId = Tenant.Id,
      O = folderData
    };

    Assert.Equal("Test", folder.O?.Title);
    Assert.Equal(5, folder.O?.ProfilesCount);
    Assert.True(folder.O?.IsFavourite);
    Assert.NotNull(folder.Jzon);
  }

  [Fact]
  public void Folder_EmptyJson_ShouldReturnDefault() {
    var folder = new Folder {
      TenantId = Tenant.TenantId,
      TenintId = Tenant.Id,
      Jzon = null
    };

    Assert.Null(folder.O);
  }

  [Fact]
  public async Task CreateProfile_ShouldReturnValidProfile() {
    var profileData = new Profilee { Title = "Test Profile", FolderId = 1 };
    var result = await PF.Tables.Profiles.Instance.Create(profileData);

    Assert.NotNull(result);
    Assert.Equal(Tenant.TenantId, result.TenantId);
    Assert.Equal(Tenant.Id, result.TenintId);
    Assert.NotNull(result.O);
    Assert.Equal("Test Profile", result.O.Title);
    Assert.Equal(1, result.O.FolderId);
  }

  [Fact]
  public async Task GetProfiles_ShouldReturnProfileCollection() {
    var result = await PF.Tables.Profiles.Instance.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task UpdateProfile_ShouldReturnUpdatedProfile() {
    var profile = new Profile {
      Id = 1,
      TenantId = Tenant.TenantId,
      TenintId = Tenant.Id,
      O = new Profilee { Title = "Updated Profile", FolderId = 1 }
    };

    var result = await PF.Tables.Profiles.Instance.Update(profile);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task DeleteProfile_ShouldReturnDeletedProfile() {
    var result = await PF.Tables.Profiles.Instance.Delete(1);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task GetProfilesPersons_ShouldReturnCollection() {
    var result = await PF.Tables.ProfilesPersons.Instance.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task GetProfilesBusinesses_ShouldReturnCollection() {
    var result = await PF.Tables.ProfilesBusinesses.Instance.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task GetProfilesAddresses_ShouldReturnCollection() {
    var result = await PF.Tables.ProfilesAddresses.Instance.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task GetProfilesLogins_ShouldReturnCollection() {
    var result = await PF.Tables.ProfilesLogins.Instance.Get();

    Assert.NotNull(result);
  }

  [Fact]
  public async Task CreateFoldersProfiles_ShouldReturnValidJunction() {
    var result = await PF.Tables.FoldersProfiles.Instance.Create(1, 2);

    Assert.NotNull(result);
    Assert.Equal(1, result.FolderId);
    Assert.Equal(2, result.ProfileId);
    Assert.Equal(Tenant.TenantId, result.TenantId);
    Assert.Equal(Tenant.Id, result.TenintId);
  }

  [Fact]
  public async Task UpdateFoldersProfiles_ShouldReturnUpdatedJunction() {
    var folderProfile = new FolderProfile(1, 2) {
      TenantId = Tenant.TenantId,
      TenintId = Tenant.Id
    };

    var result = await PF.Tables.FoldersProfiles.Instance.Update(folderProfile);

    Assert.NotNull(result);
  }

  [Fact]
  public async Task DeleteFoldersProfiles_ShouldReturnDeletedJunction() {
    var result = await PF.Tables.FoldersProfiles.Instance.Delete(new Request() {
      Body = new FolderProfile(1, 2) {
        TenantId = Tenant.TenantId,
        TenintId = Tenant.Id
      }
    });

    Assert.NotNull(result);
  }

}
