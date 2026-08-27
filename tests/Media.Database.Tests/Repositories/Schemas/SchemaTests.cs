using Media.Database.Repositories.Schemas;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Repositories.Schemas;

[TestFixture]
public class ISchemaTests
{
    [Test]
    public void ISchema_Should_Be_An_Interface()
    {
        // Assert
        typeof(ISchema).IsInterface.ShouldBeTrue();
    }

    [Test]
    public void ISchema_Should_Be_Implemented_By_ColumnsSql()
    {
        // Assert
        typeof(ColumnsSql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void ISchema_Should_Be_Implemented_By_ColumnsCql()
    {
        // Assert
        typeof(ColumnsCql).GetInterfaces().ShouldContain(typeof(ISchema));
    }
}

[TestFixture]
public class ColumnsSqlTests
{
    [Test]
    public void ColumnsSql_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(ColumnsSql).BaseType.ShouldNotBeNull();
        typeof(ColumnsSql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void ColumnsSql_Should_Implement_ISchema()
    {
        // Assert
        typeof(ColumnsSql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void ColumnsSql_Should_Have_Id_Column()
    {
        // Assert
        ColumnsSql.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_CameFromFileId_Column()
    {
        // Assert
        ColumnsSql.CameFromFileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_FileId_Column()
    {
        // Assert
        ColumnsSql.FileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_InsertedOn_Column()
    {
        // Assert
        ColumnsSql.InsertedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_IsCurrent_Column()
    {
        // Assert
        ColumnsSql.IsCurrent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_IsProperName_Column()
    {
        // Assert
        ColumnsSql.IsProperName.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_LastFileUpdate_Column()
    {
        // Assert
        ColumnsSql.LastFileUpdate.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_Metadata_Column()
    {
        // Assert
        ColumnsSql.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_Origin_Column()
    {
        // Assert
        ColumnsSql.Origin.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_OriginalFilePath_Column()
    {
        // Assert
        ColumnsSql.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_SourceMachineId_Column()
    {
        // Assert
        ColumnsSql.SourceMachineId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_UpdatedOn_Column()
    {
        // Assert
        ColumnsSql.UpdatedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_Word_Column()
    {
        // Assert
        ColumnsSql.Word.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsSql_Should_Have_WordId_Column()
    {
        // Assert
        ColumnsSql.WordId.ShouldNotBeNullOrEmpty();
    }
}

[TestFixture]
public class ColumnsCqlTests
{
    [Test]
    public void ColumnsCql_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(ColumnsCql).BaseType.ShouldNotBeNull();
        typeof(ColumnsCql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void ColumnsCql_Should_Implement_ISchema()
    {
        // Assert
        typeof(ColumnsCql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void ColumnsCql_Should_Have_Id_Column()
    {
        // Assert
        ColumnsCql.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_InsertedOn_Column()
    {
        // Assert
        ColumnsCql.InsertedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_IsCurrent_Column()
    {
        // Assert
        ColumnsCql.IsCurrent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_LastFileUpdate_Column()
    {
        // Assert
        ColumnsCql.LastFileUpdate.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_Metadata_Column()
    {
        // Assert
        ColumnsCql.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_OriginalFilePath_Column()
    {
        // Assert
        ColumnsCql.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_SourceMachineId_Column()
    {
        // Assert
        ColumnsCql.SourceMachineId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsCql_Should_Have_UpdatedOn_Column()
    {
        // Assert
        ColumnsCql.UpdatedOn.ShouldNotBeNullOrEmpty();
    }
}

[TestFixture]
public class BaseSchemaTests
{
    [Test]
    public void BaseSchema_Should_Be_Abstract()
    {
        // Assert
        typeof(BaseSchema<,>).IsAbstract.ShouldBeTrue();
    }

    [Test]
    public void BaseSchema_Should_Be_Generic_With_Two_Type_Parameters()
    {
        // Assert
        typeof(BaseSchema<,>).IsGenericTypeDefinition.ShouldBeTrue();
        typeof(BaseSchema<,>).GetGenericArguments().Length.ShouldBe(2);
    }

    [Test]
    public void BaseSchema_Should_Implement_ISchema()
    {
        // Assert
        typeof(BaseSchema<,>).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void BaseSchema_Should_Have_TParent_Constraint()
    {
        // Arrange
        var typeParams = typeof(BaseSchema<,>).GetGenericArguments();
        var tParent = typeParams[0];

        // Assert
        var constraints = tParent.GetGenericParameterConstraints();
        constraints.Length.ShouldBeGreaterThan(0);
    }

    [Test]
    public void BaseSchema_Should_Have_TChild_Constraint()
    {
        // Arrange
        var typeParams = typeof(BaseSchema<,>).GetGenericArguments();
        var tChild = typeParams[1];

        // Assert
        var constraints = tChild.GetGenericParameterConstraints();
        constraints.ShouldContain(typeof(ISchema));
    }

    [Test]
    public void BaseSchema_Should_Support_ColumnsSql_Inheritance()
    {
        // Arrange
        var columnsSql = new ColumnsSql();

        // Assert
        columnsSql.ShouldNotBeNull();
        columnsSql.ShouldBeAssignableTo<ISchema>();
    }

    [Test]
    public void BaseSchema_Should_Support_ColumnsCql_Inheritance()
    {
        // Arrange
        var columnsCql = new ColumnsCql();

        // Assert
        columnsCql.ShouldNotBeNull();
        columnsCql.ShouldBeAssignableTo<ISchema>();
    }
}

