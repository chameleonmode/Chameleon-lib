using Chameleon.lib;
using Chameleon.lib.Abs;
using Chameleon.lib.Abs.Platformatic;

namespace Tests.Abs;

public interface IDT {
  public int? Id { get; init; }
  public string TenantId { get; init; }
  public string? Jzon { get; set; }
}
public record DT<T> : IDT {
  public int? Id { get; init; }
  public required string TenantId { get; init; }
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
public record Identitee : EE {
  public required int ProfileId { get; init; }
}

public record Folder : DT<Folderee>;
public record Folderee(int ProfilesCount = 0) : EE;

public record Profile : DT<Profilee>;
public record Profilee(int FolderId = -1) : EE {
  public record Proxzy(string? Host = null, int Port = 0, string? UserName = null, string? Password = null);
  public Proxzy Proxy { get; set; } = new();
}

public record ProfilePersons : DT<Personee>;
public record Personee : Identitee {
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

public record ProfileBuisnesses : DT<Buisnessesee>;
public record Buisnessesee : Identitee {
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

public abstract class Table<T>(string prefix) : Web where T : IDT {
  public string Prefix { get; } = '/' + prefix + '/';
  public async Task<IEnumerable<T>?> Get() => await Get<IEnumerable<T>?>(Prefix);
  public async Task<T?> Create(Request request) => await Post<T>(Prefix, request);
  public async Task<T?> Update(T dt) => await Put<T?>($"{Prefix}{dt.Id}", new(Body: dt));
  public async Task<T?> Delete(int id) => await Delete<T?>($"{Prefix}{id}");
}
public class PF {
  public static class Tables {
    public class Folders : Table<Folder> {
      Folders() : base("folders") { }
      public async Task<Folder?> Create(string title) {
        var datas = await Create(new Request(
          Body: new Folder {
            TenantId = DB.Instance.DBuser!.TenantId,
            O = new Folderee { Title = title }
          }
        ));
        return datas;
      }
      public static Folders Instance { get; } = new();
    }
  }

  PF() { }
  public static PF Instance { get; } = new();
}

public class Tests : TestSetup {
  public string TenantId { get; } = "b6633ec1-138f-4ec6-b9d0-71b0660c0a42";
  public string UserId { get; } = "b6633ec1-138f-4ec6-b9d0-71b0660c0a41";
  public Tests() {
    DB.Instance.DBuser = new(1, UserId, "a@a.com", null, TenantId, "", null, DateTime.Now, DateTime.Now);
  }
  [Fact]
  public async Task CreateFolder_ShouldReturnValidFolder() {
    var result = await PF.Tables.Folders.Instance.Create("Test Folder");
    
    Assert.NotNull(result);
    Assert.Equal(TenantId, result.TenantId);
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
      TenantId = TenantId,
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
      TenantId = TenantId,
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
      TenantId = TenantId,
      Jzon = null
    };

    Assert.Null(folder.O);
  }

}
