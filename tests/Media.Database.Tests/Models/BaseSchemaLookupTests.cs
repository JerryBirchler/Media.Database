using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class BaseSchemaLookupTests
{
    [Test]
    public void This_Should_Initialize_With_Default_Values()
    {
        // Act
        var lookup = new BaseSchemaLookup();

        // Assert
        lookup.HasFormatter.ShouldBeNull();
        lookup.Names.ShouldNotBeNull();
        lookup.Names.ShouldBeEmpty();
    }

    [Test]
    public void This_Should_Allow_Setting_HasFormatter()
    {
        // Arrange
        var lookup = new BaseSchemaLookup();

        // Act
        lookup.HasFormatter = true;

        // Assert
        lookup.HasFormatter.ShouldBe(true);
    }

    [Test]
    public void This_Should_Allow_Adding_To_Names_Dictionary()
    {
        // Arrange
        var lookup = new BaseSchemaLookup();

        // Act
        lookup.Names["Field1"] = "Value1";
        lookup.Names["Field2"] = "Value2";

        // Assert
        lookup.Names.Count.ShouldBe(2);
        lookup.Names["Field1"].ShouldBe("Value1");
        lookup.Names["Field2"].ShouldBe("Value2");
    }
}

[TestFixture]
public class BaseWordRequestTests
{
    [Test, AutoData]
    public void This_Should_Allow_PropertyAssignment(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new BaseWordRequest
        {
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            CameFromFileId = fileId,
            Action = KafkaProducerActions.Add
        };

        // Assert
        request.Word.ShouldBe(word);
        request.Origin.ShouldBe(origin);
        request.IsProperName.ShouldBe(isProperName);
        request.CameFromFileId.ShouldBe(fileId);
        request.Action.ShouldBe(KafkaProducerActions.Add);
    }

    [Test]
    public void This_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var request = new BaseWordRequest
        {
            Word = "test",
            Origin = WordOrigin.Name,
            IsProperName = false,
            CameFromFileId = Guid.NewGuid()
        };

        // Assert
        request.Word.ShouldNotBeNull();
        request.CameFromFileId.ShouldNotBe(Guid.Empty);
    }

    [Test, AutoData]
    public void This_Should_Support_All_WordOrigin_Values(Guid fileId)
    {
        // Act & Assert
        foreach (WordOrigin origin in Enum.GetValues(typeof(WordOrigin)))
        {
            var request = new BaseWordRequest
            {
                Word = "test",
                Origin = origin,
                IsProperName = false,
                CameFromFileId = fileId
            };

            request.Origin.ShouldBe(origin);
        }
    }
}

[TestFixture]
public class DeleteWordRequestTests
{
    [Test, AutoData]
    public void This_Should_Inherit_From_BaseWordRequest(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new DeleteWordRequest
        {
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            CameFromFileId = fileId
        };

        // Assert
        request.ShouldBeAssignableTo<BaseWordRequest>();
    }

    [Test, AutoData]
    public void This_Should_Default_Action_To_Delete(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new DeleteWordRequest
        {
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            CameFromFileId = fileId
        };

        // Assert
        request.Action.ShouldBe(KafkaProducerActions.Delete);
    }

    [Test, AutoData]
    public void This_Should_Allow_Property_Assignment(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new DeleteWordRequest
        {
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            CameFromFileId = fileId
        };

        // Assert
        request.Word.ShouldBe(word);
        request.Origin.ShouldBe(origin);
        request.IsProperName.ShouldBe(isProperName);
        request.CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void This_Should_Allow_Overriding_Action(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new DeleteWordRequest
        {
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            CameFromFileId = fileId,
            Action = KafkaProducerActions.Update
        };

        // Assert
        request.Action.ShouldBe(KafkaProducerActions.Update);
    }
}
