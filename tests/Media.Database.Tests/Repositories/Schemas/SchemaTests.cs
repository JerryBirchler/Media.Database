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
    public void ISchema_Should_Be_Implemented_By_ColumnsNoSql()
    {
        // Assert
        typeof(ColumnsNoSql).GetInterfaces().ShouldContain(typeof(ISchema));
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
public class ColumnsNoSqlTests
{
    [Test]
    public void ColumnsNoSql_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(ColumnsNoSql).BaseType.ShouldNotBeNull();
        typeof(ColumnsNoSql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void ColumnsNoSql_Should_Implement_ISchema()
    {
        // Assert
        typeof(ColumnsNoSql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void ColumnsNoSql_Should_Have_Id_Column()
    {
        // Assert
        ColumnsNoSql.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_InsertedOn_Column()
    {
        // Assert
        ColumnsNoSql.InsertedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_IsCurrent_Column()
    {
        // Assert
        ColumnsNoSql.IsCurrent.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_LastFileUpdate_Column()
    {
        // Assert
        ColumnsNoSql.LastFileUpdate.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_Metadata_Column()
    {
        // Assert
        ColumnsNoSql.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_OriginalFilePath_Column()
    {
        // Assert
        ColumnsNoSql.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_SourceMachineId_Column()
    {
        // Assert
        ColumnsNoSql.SourceMachineId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void ColumnsNoSql_Should_Have_UpdatedOn_Column()
    {
        // Assert
        ColumnsNoSql.UpdatedOn.ShouldNotBeNullOrEmpty();
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
    public void BaseSchema_Should_Support_ColumnsNoSql_Inheritance()
    {
        // Arrange
        var columnsNoSql = new ColumnsNoSql();

        // Assert
        columnsNoSql.ShouldNotBeNull();
        columnsNoSql.ShouldBeAssignableTo<ISchema>();
    }
}

