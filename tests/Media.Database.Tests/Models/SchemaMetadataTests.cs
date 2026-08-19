using System;
using System.Collections.Generic;
using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class SchemaMetadataTests
{
    [Test]
    public void This_Should_Initialize_With_Null_Values()
    {
        // Act
        var metadata = new SchemaMetadata();

        // Assert
        metadata.FormatDelegate.ShouldBeNull();
        metadata.SubFields.ShouldBeNull();
    }

    [Test]
    public void This_Should_Allow_Setting_FormatDelegate()
    {
        // Arrange
        var metadata = new SchemaMetadata();
        Func<string, string> formatter = (s) => s.ToUpper();

        // Act
        metadata.FormatDelegate = formatter;

        // Assert
        metadata.FormatDelegate.ShouldNotBeNull();
        metadata.FormatDelegate("test").ShouldBe("TEST");
    }

    [Test]
    public void This_Should_Allow_Setting_Types_And_Values()
    {
        // Arrange
        var metadata = new SchemaMetadata();
        var parentLookup = new BaseSchemaLookup();
        var childLookup = new BaseSchemaLookup();

        // Act
        metadata.ParentType = typeof(string);
        metadata.ParentValue = parentLookup;
        metadata.ChildType = typeof(int);
        metadata.ChildValue = childLookup;

        // Assert
        metadata.ParentType.ShouldBe(typeof(string));
        metadata.ParentValue.ShouldBe(parentLookup);
        metadata.ChildType.ShouldBe(typeof(int));
        metadata.ChildValue.ShouldBe(childLookup);
    }

    [Test, AutoData]
    public void This_Should_Allow_Setting_SubFields(object subFields)
    {
        // Arrange
        var metadata = new SchemaMetadata();

        // Act
        metadata.SubFields = subFields;

        // Assert
        metadata.SubFields.ShouldBe(subFields);
    }

    [Test]
    public void This_Should_Store_BaseSchemaLookup_References()
    {
        // Arrange
        var metadata = new SchemaMetadata();
        var lookup1 = new BaseSchemaLookup { HasFormatter = true };
        var lookup2 = new BaseSchemaLookup { HasFormatter = false };

        // Act
        metadata.ParentValue = lookup1;
        metadata.ChildValue = lookup2;

        // Assert
        metadata.ParentValue.HasFormatter.ShouldBe(true);
        metadata.ChildValue.HasFormatter.ShouldBe(false);
    }
}

[TestFixture]
public class UpdateFileResponseTests
{
    [Test]
    public void This_Should_Initialize_With_Empty_Updates()
    {
        // Act
        var response = new UpdateFileResponse();

        // Assert
        response.File.ShouldBeNull();
        response.Updates.ShouldNotBeNull();
        response.Updates.ShouldBeEmpty();
    }

    [Test, AutoData]
    public void This_Should_Allow_Setting_File(Files file)
    {
        // Act
        var response = new UpdateFileResponse { File = file };

        // Assert
        response.File.ShouldBe(file);
    }

    [Test]
    public void This_Should_Allow_Adding_Updates()
    {
        // Arrange
        var response = new UpdateFileResponse();
        var update1 = new ChangeWordRequest
        {
            NewSpan = "new1",
            Origin = WordOrigin.Name,
            CameFromFileId = Guid.NewGuid()
        };
        var update2 = new ChangeWordRequest
        {
            NewSpan = "new2",
            Origin = WordOrigin.FromTitle,
            CameFromFileId = Guid.NewGuid()
        };

        // Act
        response.Updates.Add(update1);
        response.Updates.Add(update2);

        // Assert
        response.Updates.Count.ShouldBe(2);
        response.Updates[0].ShouldBe(update1);
        response.Updates[1].ShouldBe(update2);
    }

    [Test]
    public void This_Should_Support_Null_File_With_Updates()
    {
        // Arrange
        var update = new ChangeWordRequest
        {
            NewSpan = "test",
            Origin = WordOrigin.Name,
            CameFromFileId = Guid.NewGuid()
        };

        // Act
        var response = new UpdateFileResponse
        {
            File = null,
            Updates = new List<ChangeWordRequest> { update }
        };

        // Assert
        response.File.ShouldBeNull();
        response.Updates.Count.ShouldBe(1);
    }
}

[TestFixture]
public class UploadFileRequestTests
{
    [Test, AutoData]
    public void This_Should_Allow_PropertyAssignment(
        int sourceMachineId,
        string originalFilePath,
        DateTimeOffset lastFileUpdate,
        Metadata metadata)
    {
        // Act
        var request = new UploadFileRequest
        {
            SourceMachineId = sourceMachineId,
            OriginalFilePath = originalFilePath,
            LastFileUpdate = lastFileUpdate,
            Metadata = metadata
        };

        // Assert
        request.SourceMachineId.ShouldBe(sourceMachineId);
        request.OriginalFilePath.ShouldBe(originalFilePath);
        request.LastFileUpdate.ShouldBe(lastFileUpdate);
        request.Metadata.ShouldBe(metadata);
    }

    [Test]
    public void This_Should_Allow_Null_LastFileUpdate()
    {
        // Act
        var request = new UploadFileRequest
        {
            SourceMachineId = 1,
            OriginalFilePath = "/path/to/file.txt",
            LastFileUpdate = null,
            Metadata = null
        };

        // Assert
        request.LastFileUpdate.ShouldBeNull();
    }

    [Test]
    public void This_Should_Allow_Null_Metadata()
    {
        // Act
        var request = new UploadFileRequest
        {
            SourceMachineId = 1,
            OriginalFilePath = "/path/to/file.txt",
            Metadata = null
        };

        // Assert
        request.Metadata.ShouldBeNull();
    }

    [Test, AutoData]
    public void This_Should_Have_Required_Properties(
        int sourceMachineId,
        string originalFilePath)
    {
        // Act
        var request = new UploadFileRequest
        {
            SourceMachineId = sourceMachineId,
            OriginalFilePath = originalFilePath
        };

        // Assert
        request.SourceMachineId.ShouldNotBe(0);
        request.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void This_Should_Support_DateTimeOffset_Values()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var request = new UploadFileRequest
        {
            SourceMachineId = 1,
            OriginalFilePath = "/test/file.txt",
            LastFileUpdate = now
        };

        // Assert
        request.LastFileUpdate.ShouldNotBeNull();
        request.LastFileUpdate.Value.ShouldBe(now);
    }

    [Test, AutoData]
    public void This_Should_Store_Metadata_Object(Metadata metadata)
    {
        // Act
        var request = new UploadFileRequest
        {
            SourceMachineId = 1,
            OriginalFilePath = "/test/file.txt",
            Metadata = metadata
        };

        // Assert
        request.Metadata.ShouldBe(metadata);
        request.Metadata.ShouldNotBeNull();
    }
}
