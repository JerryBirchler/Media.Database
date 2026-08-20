using Media.Database.Repositories.Schemas;
using NUnit.Framework;
using Shouldly;
using System.Linq;

namespace Media.Database.Tests.Repositories.Schemas;

[TestFixture]
public class NoSubFieldsTests
{
    [Test]
    public void This_Should_Be_Sealed()
    {
        // Assert
        typeof(NoSubFields).IsSealed.ShouldBeTrue();
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(NoSubFields).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Be_Instantiable()
    {
        // Act
        var noSubFields = new NoSubFields();

        // Assert
        noSubFields.ShouldNotBeNull();
    }

    [Test]
    public void This_Should_Have_No_Public_Members()
    {
        // Assert - Exclude constructors since sealed classes have default constructors
        var publicMembers = typeof(NoSubFields)
            .GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where((System.Reflection.MemberInfo m) => !m.Name.Equals(".ctor"))
            .ToArray();
        publicMembers.Length.ShouldBe(0);
    }
}

[TestFixture]
public class OrdinalsTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(Ordinals).BaseType.ShouldNotBeNull();
        typeof(Ordinals).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(Ordinals).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_NoSubFields_As_Child_Type()
    {
        // Arrange - Verifying through base type generic arguments
        var baseType = typeof(Ordinals).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs.Length.ShouldBe(2);
        genericArgs[1].ShouldBe(typeof(NoSubFields));
    }

    [Test]
    public void This_Should_Have_Id_Field()
    {
        // Assert
        Ordinals.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_CameFromFileId_Field()
    {
        // Assert
        Ordinals.CameFromFileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_FileId_Field()
    {
        // Assert
        Ordinals.FileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_InsertedOn_Field()
    {
        // Assert
        Ordinals.InsertedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_IsCurrent_Field()
    {
        // Assert
        Ordinals.IsCurrent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_IsProperName_Field()
    {
        // Assert
        Ordinals.IsProperName.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_LastFileUpdate_Field()
    {
        // Assert
        Ordinals.LastFileUpdate.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Limit_Field()
    {
        // Assert
        Ordinals.Limit.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Metadata_Field()
    {
        // Assert
        Ordinals.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Origin_Field()
    {
        // Assert
        Ordinals.Origin.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_OriginalFilePath_Field()
    {
        // Assert
        Ordinals.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_SourceMachineId_Field()
    {
        // Assert
        Ordinals.SourceMachineId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_UpdatedOn_Field()
    {
        // Assert
        Ordinals.UpdatedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Word_Field()
    {
        // Assert
        Ordinals.Word.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_WordId_Field()
    {
        // Assert
        Ordinals.WordId.ShouldNotBeNullOrEmpty();
    }
}

[TestFixture]
public class OrdinalsNoSqlTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(OrdinalsNoSql).BaseType.ShouldNotBeNull();
        typeof(OrdinalsNoSql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(OrdinalsNoSql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_Ordinals_As_Child_Type()
    {
        // Arrange
        var baseType = typeof(OrdinalsNoSql).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs.Length.ShouldBe(2);
        genericArgs[1].ShouldBe(typeof(Ordinals));
    }

    [Test]
    public void This_Should_Have_Id_Field()
    {
        // Assert
        OrdinalsNoSql.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_InsertedOn_Field()
    {
        // Assert
        OrdinalsNoSql.InsertedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_IsCurrent_Field()
    {
        // Assert
        OrdinalsNoSql.IsCurrent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_LastFileUpdate_Field()
    {
        // Assert
        OrdinalsNoSql.LastFileUpdate.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Metadata_Field()
    {
        // Assert
        OrdinalsNoSql.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_OriginalFilePath_Field()
    {
        // Assert
        OrdinalsNoSql.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_SourceMachineId_Field()
    {
        // Assert
        OrdinalsNoSql.SourceMachineId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_UpdatedOn_Field()
    {
        // Assert
        OrdinalsNoSql.UpdatedOn.ShouldNotBeNullOrEmpty();
    }
}

[TestFixture]
public class OrdinalsSqlTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(OrdinalsSql).BaseType.ShouldNotBeNull();
        typeof(OrdinalsSql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(OrdinalsSql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_Ordinals_As_Child_Type()
    {
        // Arrange
        var baseType = typeof(OrdinalsSql).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs.Length.ShouldBe(2);
        genericArgs[1].ShouldBe(typeof(Ordinals));
    }

    [Test]
    public void This_Should_Have_Id_Field()
    {
        // Assert
        OrdinalsSql.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_CameFromFileId_Field()
    {
        // Assert
        OrdinalsSql.CameFromFileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_FileId_Field()
    {
        // Assert
        OrdinalsSql.FileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_InsertedOn_Field()
    {
        // Assert
        OrdinalsSql.InsertedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_IsCurrent_Field()
    {
        // Assert
        OrdinalsSql.IsCurrent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_IsProperName_Field()
    {
        // Assert
        OrdinalsSql.IsProperName.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_LastFileUpdate_Field()
    {
        // Assert
        OrdinalsSql.LastFileUpdate.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Metadata_Field()
    {
        // Assert
        OrdinalsSql.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Origin_Field()
    {
        // Assert
        OrdinalsSql.Origin.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_OriginalFilePath_Field()
    {
        // Assert
        OrdinalsSql.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_SourceMachineId_Field()
    {
        // Assert
        OrdinalsSql.SourceMachineId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_UpdatedOn_Field()
    {
        // Assert
        OrdinalsSql.UpdatedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Word_Field()
    {
        // Assert
        OrdinalsSql.Word.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_WordId_Field()
    {
        // Assert
        OrdinalsSql.WordId.ShouldNotBeNullOrEmpty();
    }
}

