using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Media.Database.Tests.Models;

[TestFixture]
public class SchemaMetadataTests
{
    [Test]
    public void SchemaMetadata_Should_Initialize_With_Null_Values()
    {
        // Act
        var metadata = new SchemaMetadata();

        // Assert
        metadata.FormatDelegate.ShouldBeNull();
        metadata.SubFields.ShouldBeNull();
    }

    [Test]
    public void SchemaMetadata_Should_Allow_Setting_FormatDelegate()
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
    public void SchemaMetadata_Should_Allow_Setting_Types_And_Values()
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
    public void SchemaMetadata_Should_Allow_Setting_SubFields(object subFields)
    {
        // Arrange
        var metadata = new SchemaMetadata();

        // Act
        metadata.SubFields = subFields;

        // Assert
        metadata.SubFields.ShouldBe(subFields);
    }

    [Test]
    public void SchemaMetadata_Should_Store_BaseSchemaLookup_References()
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
    public void UpdateFileResponse_Should_Initialize_With_Null_File()
    {
        // Act
        var response = new UpdateFileResponse();

        // Assert
        response.File.ShouldBeNull();
    }

    [Test, AutoData]
    public void UpdateFileResponse_Should_Allow_Setting_File(Files file)
    {
        // Act
        var response = new UpdateFileResponse { File = file };

        // Assert
        response.File.ShouldBe(file);
    }
}

[TestFixture]
public class UploadFileRequestTests
{
    [Test, AutoData]
    public void UploadFileRequest_Should_Allow_PropertyAssignment(
        string originalFilePath,
        DateTimeOffset lastFileUpdate,
        Metadata metadata)
    {
        // Act
        var request = new UploadFileRequest
        {
            OriginalFilePath = originalFilePath,
            LastFileUpdate = lastFileUpdate,
            Metadata = metadata
        };

        // Assert
        request.OriginalFilePath.ShouldBe(originalFilePath);
        request.LastFileUpdate.ShouldBe(lastFileUpdate);
        request.Metadata.ShouldBe(metadata);
    }

    [Test]
    public void UploadFileRequest_Should_Allow_Null_LastFileUpdate()
    {
        // Act
        var request = new UploadFileRequest
        {
            OriginalFilePath = "/path/to/file.txt",
            LastFileUpdate = null,
            Metadata = null
        };

        // Assert
        request.LastFileUpdate.ShouldBeNull();
    }

    [Test]
    public void UploadFileRequest_Should_Allow_Null_Metadata()
    {
        // Act
        var request = new UploadFileRequest
        {
            OriginalFilePath = "/path/to/file.txt",
            Metadata = null
        };

        // Assert
        request.Metadata.ShouldBeNull();
    }

    [Test, AutoData]
    public void UploadFileRequest_Should_Have_Required_Properties(
        string originalFilePath)
    {
        // Act
        var request = new UploadFileRequest
        {
            OriginalFilePath = originalFilePath
        };

        // Assert
        request.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void UploadFileRequest_Should_Support_DateTimeOffset_Values()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var request = new UploadFileRequest
        {
            OriginalFilePath = "/test/file.txt",
            LastFileUpdate = now
        };

        // Assert
        request.LastFileUpdate.ShouldNotBeNull();
        request.LastFileUpdate.Value.ShouldBe(now);
    }

    [Test, AutoData]
    public void UploadFileRequest_Should_Store_Metadata_Object(Metadata metadata)
    {
        // Act
        var request = new UploadFileRequest
        {
            OriginalFilePath = "/test/file.txt",
            Metadata = metadata
        };

        // Assert
        request.Metadata.ShouldBe(metadata);
        request.Metadata.ShouldNotBeNull();
    }
}
