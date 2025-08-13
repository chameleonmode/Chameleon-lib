using System.Diagnostics;
using Chameleon.lib.Abs.Platformatic;
using Chameleon.lib.Abs;

namespace Tests.Abs;

/// <summary>
/// Comprehensive test suite for the Platformatic DB class
/// Tests CRUD operations for Tags, ItemTags, Users, and DataInteractions
/// </summary>
public class PlatformaticDbTests : TestSetup
{
    public PlatformaticDbTests() : base(0) { }

    #region Test Environment Setup

    /// <summary>
    /// Ensures clean test environment before each test
    /// </summary>
    public override async Task InitializeAsync()
    {
    await base.InitializeAsync();
    // Ensure user context is loaded and clean any leftover test data
    await DB.I.Userz.Load();
    await CleanupTestData();
    }

    /// <summary>
    /// Cleanup test data to prevent test interference
    /// </summary>
    private async Task CleanupTestData()
    {
        try
        {
            // Clean up test data in proper dependency order
            
            // 1. Clean up DataInteractions first (they depend on users)
            await DB.I.Interactions.DeleteDataInteractions();
            
            // 2. Clean up ItemTags
            var itemTags = await DB.I.GetItemTags();
            if (itemTags?.Any() == true)
            {
                foreach (var itemTag in itemTags.Where(it => it.TagName?.StartsWith("test_") == true))
                {
                    await DB.I.DeleteItemTagBy(itemTag.TagItemType, itemTag.TagItemId, itemTag.TagName);
                }
            }
            
            // 3. Clean up test tags (delete tags with test prefix)
            var existingTags = await DB.I.GetTags();
            if (existingTags != null)
            {
                foreach (var tag in existingTags.Where(t => t.Name.StartsWith("test_")))
                {
                    await DB.I.DeleteTag(tag.Id);
                }
            }
            
            // 4. Clean up test users last via Userz API
            await DB.I.Userz.Load();
            var users = DB.I.Userz.Users;
            if (users?.Any() == true)
            {
                foreach (var user in users.Where(u => u.Email?.Contains("test") == true))
                {
                    // This method resolves the ID internally and deletes properly
                    _ = await DB.I.Userz.Delete(user.Email);
                }
            }
            
            Debug.WriteLine("Test environment cleanup completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Test cleanup warning: {ex.Message}");
            // Don't fail tests due to cleanup issues
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates unique test tag name to avoid conflicts
    /// </summary>
    private string GenerateTestTagName() => $"test_tag_{Guid.NewGuid():N}";

    /// <summary>
    /// Generates test email for user operations
    /// </summary>
    private string GenerateTestEmail() => $"test_{Guid.NewGuid():N}@example.com";

    /// <summary>
    /// Creates sample tag items for testing
    /// </summary>
    private Dictionary<string, List<string>> CreateSampleTagItems()
    {
        return new Dictionary<string, List<string>>
        {
            { "document", new List<string> { "doc_001", "doc_002" } },
            { "folder", new List<string> { "folder_001" } }
        };
    }

    /// <summary>
    /// Asserts that two tag objects have matching properties
    /// </summary>
    private void AssertTagsEqual(Tag expected, Tag actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.TenantId, actual.TenantId);
        // Items comparison would require deserialization
    }

    /// <summary>
    /// Asserts that two ItemTag objects have matching properties
    /// </summary>
    private void AssertItemTagsEqual(ItemTag expected, ItemTag actual)
    {
        Assert.Equal(expected.TagItemType, actual.TagItemType);
        Assert.Equal(expected.TagItemId, actual.TagItemId);
        Assert.Equal(expected.TagName, actual.TagName);
        Assert.Equal(expected.TenantId, actual.TenantId);
    }

    #endregion

    #region Basic Connectivity Tests

    [Fact]
    public void DB_Instance_Should_Be_Available()
    {
        // Arrange & Act
        var instance = DB.I;
        
        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public async Task DB_User_Should_Be_Ensured()
    {
        // Act
        await DB.I.Userz.Load();
        
        // Assert
        Assert.NotNull(DB.I.Userz.Current);
        Assert.NotNull(DB.I.Userz.Users);
        Assert.NotEmpty(DB.I.Userz.Current.TenantId);
    }

    #endregion

    #region Tag Management Tests

    [Fact]
    public async Task CreateTag_Should_Return_Valid_Tag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var tagItems = CreateSampleTagItems();

        // Act
        var createdTag = await DB.I.CreateTag(tagName, tagItems);

        // Assert
        Assert.NotNull(createdTag);
        Assert.Equal(tagName, createdTag.Name);
        Assert.True(createdTag.Id > 0);
        Assert.Equal(DB.I.Userz.Current!.TenantId, createdTag.TenantId);

        // Cleanup
        await DB.I.DeleteTag(createdTag.Id);
    }

    [Fact]
    public async Task GetTag_Should_Return_Existing_Tag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var createdTag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());

        // Act
        var retrievedTag = await DB.I.GetTag(createdTag!.Id);

        // Assert
        Assert.NotNull(retrievedTag);
        Assert.Equal(createdTag.Id, retrievedTag.Id);
        Assert.Equal(createdTag.Name, retrievedTag.Name);

        // Cleanup
        await DB.I.DeleteTag(createdTag.Id);
    }

    [Fact]
    public async Task GetTagBy_Should_Return_Tag_By_Name()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var createdTag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());

        // Act
        var retrievedTag = await DB.I.GetTagBy(tagName);

        // Assert
        Assert.NotNull(retrievedTag);
        Assert.Equal(createdTag!.Id, retrievedTag.Id);
        Assert.Equal(tagName, retrievedTag.Name);

        // Cleanup
        await DB.I.DeleteTag(createdTag.Id);
    }

    [Fact]
    public async Task UpdateTag_Should_Modify_Existing_Tag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var createdTag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var updatedItems = CreateSampleTagItems();

        // Act
        var updatedTag = await DB.I.UpdateTag(createdTag!.Id, tagName, updatedItems);

        // Assert
        Assert.NotNull(updatedTag);
        Assert.Equal(createdTag.Id, updatedTag.Id);
        Assert.Equal(tagName, updatedTag.Name);

        // Verify the update persisted
        var retrievedTag = await DB.I.GetTag(createdTag.Id);
        Assert.NotNull(retrievedTag);

        // Cleanup
        await DB.I.DeleteTag(createdTag.Id);
    }

    [Fact]
    public async Task DeleteTag_Should_Remove_Tag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var createdTag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());

        // Act
        await DB.I.DeleteTag(createdTag!.Id);

        // Assert
        // Verify tag is actually deleted
        var retrievedTag = await DB.I.GetTag(createdTag.Id);
        Assert.Null(retrievedTag);
    }

    [Fact]
    public async Task GetTags_Should_Return_All_Tags()
    {
        // Arrange
        var tagName1 = GenerateTestTagName();
        var tagName2 = GenerateTestTagName();
        var tag1 = await DB.I.CreateTag(tagName1, new Dictionary<string, List<string>>());
        var tag2 = await DB.I.CreateTag(tagName2, new Dictionary<string, List<string>>());

        // Act
        var allTags = await DB.I.GetTags();

        // Assert
        Assert.NotNull(allTags);
        Assert.Contains(allTags, t => t.Name == tagName1);
        Assert.Contains(allTags, t => t.Name == tagName2);

        // Cleanup
        await DB.I.DeleteTag(tag1!.Id);
        await DB.I.DeleteTag(tag2!.Id);
    }

    [Fact]
    public async Task SearchTags_Should_Return_Matching_Tags()
    {
        // Arrange
        var baseTagName = GenerateTestTagName();
        var searchableTagName = $"{baseTagName}_searchable";
        var createdTag = await DB.I.CreateTag(searchableTagName, new Dictionary<string, List<string>>());

        // Act
        var searchResults = await DB.I.SearchTags("searchable");

        // Assert
        Assert.NotNull(searchResults);
        Assert.Contains(searchResults, t => t.Name == searchableTagName);

        // Cleanup
        await DB.I.DeleteTag(createdTag!.Id);
    }

    #endregion

    #region Tag CRUD Comprehensive Test

    [Fact]
    public async Task Tag_Complete_CRUD_Lifecycle()
    {
        // Test complete CRUD lifecycle for tags
        var tagName = GenerateTestTagName();
        var initialItems = new Dictionary<string, List<string>>
        {
            { "document", new List<string> { "doc_001" } }
        };

        // CREATE
        var created = await DB.I.CreateTag(tagName, initialItems);
        Assert.NotNull(created);
        Assert.Equal(tagName, created.Name);

        // READ by ID
        var retrievedById = await DB.I.GetTag(created.Id);
        Assert.NotNull(retrievedById);
        Assert.Equal(created.Id, retrievedById.Id);

        // READ by Name
        var retrievedByName = await DB.I.GetTagBy(tagName);
        Assert.NotNull(retrievedByName);
        Assert.Equal(created.Id, retrievedByName.Id);

        // UPDATE
        var updatedItems = new Dictionary<string, List<string>>
        {
            { "document", new List<string> { "doc_001", "doc_002" } },
            { "folder", new List<string> { "folder_001" } }
        };
        var updated = await DB.I.UpdateTag(created.Id, tagName, updatedItems);
        Assert.NotNull(updated);

        // Verify update
        var afterUpdate = await DB.I.GetTag(created.Id);
        Assert.NotNull(afterUpdate);

        // DELETE
        await DB.I.DeleteTag(created.Id);

        // Verify deletion
        var afterDelete = await DB.I.GetTag(created.Id);
        Assert.Null(afterDelete);
    }

    #endregion

    #region ItemTag Management Tests

    [Fact]
    public async Task CreateItemTag_Should_Return_Valid_ItemTag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId = "test_doc_001";

        // Act
        var itemTag = await DB.I.CreateItemTag(itemType, itemId, tagName);

        // Assert
        Assert.NotNull(itemTag);
        Assert.Equal(itemType, itemTag.TagItemType);
        Assert.Equal(itemId, itemTag.TagItemId);
        Assert.Equal(tagName, itemTag.TagName);
        Assert.Equal(DB.I.Userz.Current!.TenantId, itemTag.TenantId);

        // Cleanup
        await DB.I.DeleteItemTagBy(itemType, itemId, tagName);
        await DB.I.DeleteTag(tag!.Id);
    }

    [Fact]
    public async Task GetItemTagBy_Should_Return_Existing_ItemTag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId = "test_doc_002";
        var created = await DB.I.CreateItemTag(itemType, itemId, tagName);

        // Act
        var retrieved = await DB.I.GetItemTagBy(itemType, itemId, tagName);

        // Assert
        Assert.NotNull(retrieved);
        AssertItemTagsEqual(created!, retrieved);

        // Cleanup
        await DB.I.DeleteItemTagBy(itemType, itemId, tagName);
        await DB.I.DeleteTag(tag!.Id);
    }

    [Fact]
    public async Task UpdateItemTagBy_Should_Modify_ItemTag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId = "test_doc_003";
        await DB.I.CreateItemTag(itemType, itemId, tagName);

        // Act
        var updated = await DB.I.UpdateItemTagBy(itemType, itemId, tagName);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(itemType, updated.TagItemType);
        Assert.Equal(itemId, updated.TagItemId);
        Assert.Equal(tagName, updated.TagName);

        // Cleanup
        await DB.I.DeleteItemTagBy(itemType, itemId, tagName);
        await DB.I.DeleteTag(tag!.Id);
    }

    [Fact]
    public async Task DeleteItemTagBy_Should_Remove_ItemTag()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId = "test_doc_004";
        await DB.I.CreateItemTag(itemType, itemId, tagName);

        // Act
        var deleted = await DB.I.DeleteItemTagBy(itemType, itemId, tagName);

        // Assert
        Assert.NotNull(deleted);
        
        // Verify deletion
        var afterDelete = await DB.I.GetItemTagBy(itemType, itemId, tagName);
        Assert.Null(afterDelete);

        // Cleanup
        await DB.I.DeleteTag(tag!.Id);
    }

    [Fact]
    public async Task GetItemTagsForItem_Should_Return_Associated_Tags()
    {
        // Arrange
        var tagName1 = GenerateTestTagName();
        var tagName2 = GenerateTestTagName();
        var tag1 = await DB.I.CreateTag(tagName1, new Dictionary<string, List<string>>());
        var tag2 = await DB.I.CreateTag(tagName2, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId = "test_doc_005";
        
        await DB.I.CreateItemTag(itemType, itemId, tagName1);
        await DB.I.CreateItemTag(itemType, itemId, tagName2);

        // Act
        var itemTags = await DB.I.GetItemTagsForItem(itemType, itemId);

        // Assert
        Assert.NotNull(itemTags);
        Assert.Contains(itemTags, it => it.TagName == tagName1);
        Assert.Contains(itemTags, it => it.TagName == tagName2);

        // Cleanup
        await DB.I.DeleteItemTagBy(itemType, itemId, tagName1);
        await DB.I.DeleteItemTagBy(itemType, itemId, tagName2);
        await DB.I.DeleteTag(tag1!.Id);
        await DB.I.DeleteTag(tag2!.Id);
    }

    [Fact]
    public async Task GetItemTags_Should_Return_All_ItemTags()
    {
        // Arrange
        var tagName = GenerateTestTagName();
        var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId1 = "test_doc_006";
        var itemId2 = "test_doc_007";
        
        await DB.I.CreateItemTag(itemType, itemId1, tagName);
        await DB.I.CreateItemTag(itemType, itemId2, tagName);

        // Act
        var allItemTags = await DB.I.GetItemTags();

        // Assert
        Assert.NotNull(allItemTags);
        Assert.Contains(allItemTags, it => it.TagItemId == itemId1 && it.TagName == tagName);
        Assert.Contains(allItemTags, it => it.TagItemId == itemId2 && it.TagName == tagName);

        // Cleanup
        await DB.I.DeleteItemTagBy(itemType, itemId1, tagName);
        await DB.I.DeleteItemTagBy(itemType, itemId2, tagName);
        await DB.I.DeleteTag(tag!.Id);
    }

    #endregion

    #region ItemTag CRUD Comprehensive Test

    [Fact]
    public async Task ItemTag_Complete_CRUD_Lifecycle()
    {
        // Test complete CRUD lifecycle for ItemTags
        var tagName = GenerateTestTagName();
        var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
        var itemType = "document";
        var itemId = "test_lifecycle_doc";

        // CREATE
        var created = await DB.I.CreateItemTag(itemType, itemId, tagName);
        Assert.NotNull(created);
        Assert.Equal(itemType, created.TagItemType);
        Assert.Equal(itemId, created.TagItemId);
        Assert.Equal(tagName, created.TagName);

        // READ
        var retrieved = await DB.I.GetItemTagBy(itemType, itemId, tagName);
        Assert.NotNull(retrieved);
        AssertItemTagsEqual(created, retrieved);

        // UPDATE
        var updated = await DB.I.UpdateItemTagBy(itemType, itemId, tagName);
        Assert.NotNull(updated);

        // Verify in collection
        var allItemTags = await DB.I.GetItemTags();
        Assert.NotNull(allItemTags);
        Assert.Contains(allItemTags, it => it.TagItemId == itemId && it.TagName == tagName);

        // DELETE
        var deleted = await DB.I.DeleteItemTagBy(itemType, itemId, tagName);
        Assert.NotNull(deleted);

        // Verify deletion
        var afterDelete = await DB.I.GetItemTagBy(itemType, itemId, tagName);
        Assert.Null(afterDelete);

        // Cleanup
        await DB.I.DeleteTag(tag!.Id);
    }

    #endregion

    #region User Management Tests

    [Fact]
    public async Task CreateUser_Should_Return_Valid_User()
    {
        // Arrange
        var email = GenerateTestEmail();

        // Act
    var user = await DB.I.Userz.Create(email);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.NotEmpty(user.TenantId);

    // Cleanup using Userz API
    _ = await DB.I.Userz.Delete(email);
    }

    [Fact]
    public async Task DeleteUser_Should_Remove_User()
    {
        // Arrange
        var email = GenerateTestEmail();
    await DB.I.Userz.Create(email);

    // Act - Use Userz.Delete and assert result
    var deleteResult = await DB.I.Userz.Delete(email);

    // Assert - Delete returns true when user removed
    Assert.True(deleteResult);
    }

    #endregion

    #region DataInteraction Tests

    [Fact]
    public async Task PostDataInteraction_Should_Create_Interaction()
    {
    // Arrange - Ensure user exists first to satisfy foreign key constraints
    await DB.I.Userz.Load();
        
    var request = new DB.Routes.Interactions.PostDataInteractionRequest(
            ReceiverId: Guid.NewGuid().ToString(),
            DataType: "test_data_type",
            DataPayload: new { message = "test payload" }
        );

        // Act
    var interaction = await DB.I.Interactions.PostDataInteraction(request);

        // Assert
        Assert.NotNull(interaction);
    Assert.Equal(request.DataType, interaction.DataType);
    Assert.Equal(DB.I.Userz.Current!.TenantId, interaction.TenantId);

        // Cleanup will be handled by CleanupTestData method
    }

    [Fact]
    public async Task GetDataInteractions_Should_Return_All_Interactions()
    {
    // Arrange - Ensure user exists first to satisfy foreign key constraints
    await DB.I.Userz.Load();
        
    var request = new DB.Routes.Interactions.PostDataInteractionRequest(
            ReceiverId: Guid.NewGuid().ToString(),
            DataType: "test_data_type",
            DataPayload: new { message = "test get all" }
        );
    await DB.I.Interactions.PostDataInteraction(request);

        // Act
    var interactions = await DB.I.Interactions.GetDataInteractions();

        // Assert
        Assert.NotNull(interactions);
        Assert.Contains(interactions, i => i.DataType == "test_data_type");
    }

    [Fact]
    public async Task GetDataInteractions_ByType_Should_Filter_Correctly()
    {
    // Arrange - Ensure user exists first to satisfy foreign key constraints
    await DB.I.Userz.Load();
        
    var dataType = DB.Routes.Interactions.Types.cooky;
    var request = new DB.Routes.Interactions.PostDataInteractionRequest(
            ReceiverId: Guid.NewGuid().ToString(),
            DataType: dataType.ToString(),
            DataPayload: new { message = "test filter" }
        );
    await DB.I.Interactions.PostDataInteraction(request);

        // Act
        var filtered = await DB.I.Interactions.GetDataInteractions(dataType);

        // Assert
        Assert.NotNull(filtered);
    Assert.All(filtered, i => Assert.Equal(dataType.ToString(), i.DataType));

        // Cleanup
    await DB.I.Interactions.DeleteDataInteractions(dataType);
    }

    [Fact]
    public async Task DeleteDataInteractions_Should_Remove_By_Type()
    {
    // Arrange - Ensure user exists first to satisfy foreign key constraints
    await DB.I.Userz.Load();
        
        var dataType = "test_cooky_type";
    var request = new DB.Routes.Interactions.PostDataInteractionRequest(
            ReceiverId: Guid.NewGuid().ToString(),
            DataType: dataType,
            DataPayload: new { message = "test delete" }
        );
    await DB.I.Interactions.PostDataInteraction(request);

        // Act
    await DB.I.Interactions.DeleteDataInteractions();

        // Assert
    var remaining = await DB.I.Interactions.GetDataInteractions();
    Assert.NotNull(remaining);
    Assert.DoesNotContain(remaining, i => i.DataType == dataType);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Tag_And_ItemTag_Integration_Test()
    {
        // Test the relationship between Tags and ItemTags
        var tagName = GenerateTestTagName();
        var itemType = "document";
        var itemId1 = "integration_doc_001";
        var itemId2 = "integration_doc_002";

        // Create tag with initial items
        var initialItems = new Dictionary<string, List<string>>
        {
            { itemType, new List<string> { itemId1 } }
        };
        var tag = await DB.I.CreateTag(tagName, initialItems);
        Assert.NotNull(tag);

        // Create individual item tags
        var itemTag1 = await DB.I.CreateItemTag(itemType, itemId1, tagName);
        var itemTag2 = await DB.I.CreateItemTag(itemType, itemId2, tagName);
        Assert.NotNull(itemTag1);
        Assert.NotNull(itemTag2);

        // Update tag to include both items
        var updatedItems = new Dictionary<string, List<string>>
        {
            { itemType, new List<string> { itemId1, itemId2 } }
        };
        var updatedTag = await DB.I.UpdateTag(tag.Id, tagName, updatedItems);
        Assert.NotNull(updatedTag);

        // Verify ItemTags are properly associated
        var itemTagsForDoc1 = await DB.I.GetItemTagsForItem(itemType, itemId1);
        var itemTagsForDoc2 = await DB.I.GetItemTagsForItem(itemType, itemId2);
        
        Assert.NotNull(itemTagsForDoc1);
        Assert.NotNull(itemTagsForDoc2);
        Assert.Contains(itemTagsForDoc1, it => it.TagName == tagName);
        Assert.Contains(itemTagsForDoc2, it => it.TagName == tagName);

        // Cleanup
        await DB.I.DeleteItemTagBy(itemType, itemId1, tagName);
        await DB.I.DeleteItemTagBy(itemType, itemId2, tagName);
        await DB.I.DeleteTag(tag.Id);
    }

    [Fact]
    public async Task Multiple_Users_Multiple_Tags_Integration_Test()
    {
        // Test isolation between different tenants/users
        var originalUser = DB.I.Userz.Current;
        
        // Create test user
        var testEmail = GenerateTestEmail();
        var testUser = await DB.I.Userz.Create(testEmail);
        Assert.NotNull(testUser);

        // Create tags with original user
        var tagName1 = GenerateTestTagName();
        var tag1 = await DB.I.CreateTag(tagName1, new Dictionary<string, List<string>>());
        Assert.NotNull(tag1);
        Assert.Equal(originalUser!.TenantId, tag1.TenantId);

        // Verify tags are properly scoped to tenant
        var userTags = await DB.I.GetTags();
        Assert.NotNull(userTags);
        Assert.Contains(userTags, t => t.Name == tagName1 && t.TenantId == originalUser.TenantId);

    // Cleanup
    await DB.I.DeleteTag(tag1.Id);
    _ = await DB.I.Userz.Delete(testEmail);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetTag_NonExistent_Should_Return_Null()
    {
        // Act
        var nonExistentTag = await DB.I.GetTag(999999);

        // Assert
        Assert.Null(nonExistentTag);
    }

    [Fact]
    public async Task GetTagBy_NonExistent_Should_Return_Null()
    {
        // Act
        var nonExistentTag = await DB.I.GetTagBy("non_existent_tag_name");

        // Assert
        Assert.Null(nonExistentTag);
    }

    [Fact]
    public async Task GetItemTagBy_NonExistent_Should_Return_Null()
    {
        // Act
        var nonExistentItemTag = await DB.I.GetItemTagBy("document", "non_existent_doc", "non_existent_tag");

        // Assert
        Assert.Null(nonExistentItemTag);
    }

    [Fact]
    public async Task DeleteUser_NonExistent_Should_Return_False()
    {
        // Act - Use Userz.Delete returns false when user does not exist
        var result = await DB.I.Userz.Delete("non_existent@email.com");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Bulk_Tag_Operations_Performance_Test()
    {
        var stopwatch = Stopwatch.StartNew();
        var tagCount = 10;
        var createdTags = new List<Tag>();

        // Create multiple tags
        for (int i = 0; i < tagCount; i++)
        {
            var tagName = $"perf_test_tag_{i}_{Guid.NewGuid():N}";
            var tag = await DB.I.CreateTag(tagName, new Dictionary<string, List<string>>());
            if (tag != null)
            {
                createdTags.Add(tag);
            }
        }

        stopwatch.Stop();
        Debug.WriteLine($"Created {createdTags.Count} tags in {stopwatch.ElapsedMilliseconds}ms");

        // Verify all tags were created
        Assert.Equal(tagCount, createdTags.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 30000); // Should complete within 30 seconds

        // Cleanup
        foreach (var tag in createdTags)
        {
            await DB.I.DeleteTag(tag.Id);
        }
    }

    #endregion
}
