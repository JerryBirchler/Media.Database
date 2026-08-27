using Media.Database.Repositories.Schemas;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Repositories.Schemas;

[TestFixture]
public class ParameterNamesTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(ParameterNames).BaseType.ShouldNotBeNull();
        typeof(ParameterNames).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(ParameterNames).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_Ordinals_As_Child_Type()
    {
        // Arrange
        var baseType = typeof(ParameterNames).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs[1].ShouldBe(typeof(Ordinals));
    }

    [Test]
    public void Format_Should_Prefix_With_At_Symbol()
    {
        // Act
        var result = ParameterNames.Format("Id");

        // Assert
        result.ShouldBe("@Id");
    }

    [Test]
    public void Format_Should_Work_With_Any_String()
    {
        // Act
        var result1 = ParameterNames.Format("FileId");
        var result2 = ParameterNames.Format("Word");

        // Assert
        result1.ShouldBe("@FileId");
        result2.ShouldBe("@Word");
    }

    [Test]
    public void This_Should_Have_Id_Field()
    {
        // Assert
        ParameterNames.Id.ShouldNotBeNullOrEmpty();
        ParameterNames.Id.ShouldStartWith("@");
    }

    [Test]
    public void This_Should_Have_CameFromFileId_Field()
    {
        // Assert
        ParameterNames.CameFromFileId.ShouldNotBeNullOrEmpty();
        ParameterNames.CameFromFileId.ShouldStartWith("@");
    }

    [Test]
    public void This_Should_Have_FileId_Field()
    {
        // Assert
        ParameterNames.FileId.ShouldNotBeNullOrEmpty();
        ParameterNames.FileId.ShouldStartWith("@");
    }

    [Test]
    public void This_Should_Have_All_Fields_Prefixed_With_At()
    {
        // Assert
        ParameterNames.InsertedOn.ShouldStartWith("@");
        ParameterNames.IsCurrent.ShouldStartWith("@");
        ParameterNames.IsProperName.ShouldStartWith("@");
        ParameterNames.LastFileUpdate.ShouldStartWith("@");
        ParameterNames.Limit.ShouldStartWith("@");
        ParameterNames.Metadata.ShouldStartWith("@");
        ParameterNames.Origin.ShouldStartWith("@");
        ParameterNames.OriginalFilePath.ShouldStartWith("@");
        ParameterNames.SourceMachineId.ShouldStartWith("@");
        ParameterNames.UpdatedOn.ShouldStartWith("@");
        ParameterNames.Word.ShouldStartWith("@");
        ParameterNames.WordId.ShouldStartWith("@");
    }
}

[TestFixture]
public class TablesTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(Tables).BaseType.ShouldNotBeNull();
        typeof(Tables).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(Tables).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_NoSubFields_As_Child_Type()
    {
        // Arrange
        var baseType = typeof(Tables).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs[1].ShouldBe(typeof(NoSubFields));
    }

    [Test]
    public void This_Should_Have_Files_Table()
    {
        // Assert
        Tables.Files.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_WordFiles_Table()
    {
        // Assert
        Tables.WordFiles.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Words_Table()
    {
        // Assert
        Tables.Words.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_View_Current_Files_Table()
    {
        // Assert
        Tables.View_Current_Files.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_View_WordFiles_Table()
    {
        // Assert
        Tables.View_WordFiles.ShouldNotBeNullOrEmpty();
    }
}

[TestFixture]
public class TablesCqlTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(TablesCql).BaseType.ShouldNotBeNull();
        typeof(TablesCql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(TablesCql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_Tables_As_Child_Type()
    {
        // Arrange
        var baseType = typeof(TablesCql).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs[1].ShouldBe(typeof(Tables));
    }

    [Test]
    public void This_Should_Have_Files_Table()
    {
        // Assert
        TablesCql.Files.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Format_Should_Convert_To_Snake_Case()
    {
        // Act
        var result = TablesCql.Format("MyTableName");

        // Assert
        result.ShouldBe("my_table_name");
    }

    [Test]
    public void Format_Should_Handle_Single_Word()
    {
        // Act
        var result = TablesCql.Format("files");

        // Assert
        result.ShouldBe("files");
    }

    [Test]
    public void FilesColumns_Should_Have_Id()
    {
        // Assert
        TablesCql.FilesColumns.Id.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void FilesColumns_Should_Have_All_Columns()
    {
        // Assert
        TablesCql.FilesColumns.Id.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.SourceMachineId.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.OriginalFilePath.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.LastFileUpdate.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.IsCurrent.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.InsertedOn.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.UpdatedOn.ShouldNotBeNullOrEmpty();
        TablesCql.FilesColumns.Metadata.ShouldNotBeNullOrEmpty();
    }
}

[TestFixture]
public class TablesSqlTests
{
    [Test]
    public void This_Should_Inherit_From_BaseSchema()
    {
        // Assert
        typeof(TablesSql).BaseType.ShouldNotBeNull();
        typeof(TablesSql).BaseType!.Name.ShouldStartWith("BaseSchema");
    }

    [Test]
    public void This_Should_Implement_ISchema()
    {
        // Assert
        typeof(TablesSql).GetInterfaces().ShouldContain(typeof(ISchema));
    }

    [Test]
    public void This_Should_Use_Tables_As_Child_Type()
    {
        // Arrange
        var baseType = typeof(TablesSql).BaseType;

        // Assert
        baseType.ShouldNotBeNull();
        var genericArgs = baseType!.GetGenericArguments();
        genericArgs[1].ShouldBe(typeof(Tables));
    }

    [Test]
    public void This_Should_Have_Files_Table()
    {
        // Assert
        TablesSql.Files.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_Words_Table()
    {
        // Assert
        TablesSql.Words.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_WordFiles_Table()
    {
        // Assert
        TablesSql.WordFiles.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_View_Current_Files_Table()
    {
        // Assert
        TablesSql.View_Current_Files.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Have_View_WordFiles_Table()
    {
        // Assert
        TablesSql.View_WordFiles.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void FilesColumns_Should_Have_All_Columns()
    {
        // Assert
        TablesSql.FilesColumns.Id.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.SourceMachineId.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.OriginalFilePath.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.LastFileUpdate.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.IsCurrent.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.InsertedOn.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.UpdatedOn.ShouldNotBeNullOrEmpty();
        TablesSql.FilesColumns.Metadata.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void WordsColumns_Should_Have_All_Columns()
    {
        // Assert
        TablesSql.WordsColumns.Id.ShouldNotBeNullOrEmpty();
        TablesSql.WordsColumns.Word.ShouldNotBeNullOrEmpty();
        TablesSql.WordsColumns.Origin.ShouldNotBeNullOrEmpty();
        TablesSql.WordsColumns.IsProperName.ShouldNotBeNullOrEmpty();
        TablesSql.WordsColumns.CameFromFileId.ShouldNotBeNullOrEmpty();
        TablesSql.WordsColumns.InsertedOn.ShouldNotBeNullOrEmpty();
        TablesSql.WordsColumns.UpdatedOn.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void WordFilesColumns_Should_Have_All_Columns()
    {
        // Assert
        TablesSql.WordFilesColumns.Origin.ShouldNotBeNullOrEmpty();
        TablesSql.WordFilesColumns.WordId.ShouldNotBeNullOrEmpty();
        TablesSql.WordFilesColumns.FileId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void View_WordFilesColumns_Should_Have_All_Columns()
    {
        // Assert
        TablesSql.View_WordFilesColumns.Origin.ShouldNotBeNullOrEmpty();
        TablesSql.View_WordFilesColumns.WordId.ShouldNotBeNullOrEmpty();
        TablesSql.View_WordFilesColumns.Word.ShouldNotBeNullOrEmpty();
        TablesSql.View_WordFilesColumns.FileId.ShouldNotBeNullOrEmpty();
        TablesSql.View_WordFilesColumns.IsCurrent.ShouldNotBeNullOrEmpty();
        TablesSql.View_WordFilesColumns.IsProperName.ShouldNotBeNullOrEmpty();
    }
}

