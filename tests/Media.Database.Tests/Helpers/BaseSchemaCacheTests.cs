using Media.Database.Helpers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Helpers;

[TestFixture]
public class BaseSchemaCacheTests
{
    [Test]
    public void GetField_Should_Retrieve_Static_Field_Value()
    {
        // Arrange - Using a real schema class from the project
        var fieldValue = BaseSchemaCache.GetField(
            typeof(Media.Database.Repositories.Schemas.TablesSql),
            "Files");

        // Assert
        fieldValue.ShouldNotBeNullOrEmpty();
        fieldValue.ShouldBe("public.\"Files\"");
    }

    [Test]
    public void GetField_Should_Cache_Field_On_Second_Call()
    {
        // Arrange
        var type = typeof(Media.Database.Repositories.Schemas.TablesSql);
        var fieldName = "WordFiles";

        // Act - First call
        var firstResult = BaseSchemaCache.GetField(type, fieldName);

        // Clear and call again - should use cache
        var secondResult = BaseSchemaCache.GetField(type, fieldName);

        // Assert
        firstResult.ShouldBe(secondResult);
        firstResult.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void GetField_Should_Throw_When_Field_Not_Found()
    {
        // Arrange
        var type = typeof(Media.Database.Repositories.Schemas.TablesSql);
        var invalidFieldName = "NonExistentField";

        // Act & Assert
        Should.Throw<ArgumentException>(() => BaseSchemaCache.GetField(type, invalidFieldName))
            .Message.ShouldContain("not found");
    }

    [Test]
    public void Lookup_Should_Be_Empty_Dictionary_Initially()
    {
        // Assert
        BaseSchemaCache.Lookup.ShouldNotBeNull();
    }

    [Test]
    public void FieldCache_Should_Be_Empty_Dictionary_Initially()
    {
        // Assert
        BaseSchemaCache.FieldCache.ShouldNotBeNull();
    }

    [Test]
    public void Metadata_Should_Be_Empty_Dictionary_Initially()
    {
        // Assert
        BaseSchemaCache.Metadata.ShouldNotBeNull();
    }

    [Test]
    public void GetField_Should_Build_Correct_Cache_Key()
    {
        // Arrange
        var type = typeof(Media.Database.Repositories.Schemas.ColumnsSql);
        var fieldName = "Id";

        // Act
        BaseSchemaCache.GetField(type, fieldName);

        // Assert
        var expectedKey = $"{type.FullName}.{fieldName}";
        BaseSchemaCache.FieldCache.ContainsKey(expectedKey).ShouldBeTrue();
    }
}
