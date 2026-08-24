using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class UpsertWordRequestTests
{
    [Test, AutoData]
    public void UpsertWordRequest_Should_Inherit_From_BaseWordRequest(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new UpsertWordRequest
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
    public void UpsertWordRequest_Should_Default_Action_To_Upsert(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new UpsertWordRequest
        {
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            CameFromFileId = fileId
        };

        // Assert
        request.Action.ShouldBe(KafkaProducerActions.Upsert);
    }

    [Test, AutoData]
    public void UpsertWordRequest_Should_Allow_Property_Assignment(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new UpsertWordRequest
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
    public void UpsertWordRequest_Should_Allow_Overriding_Action(
        string word,
        WordOrigin origin,
        bool isProperName,
        Guid fileId)
    {
        // Act
        var request = new UpsertWordRequest
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

[TestFixture]
public class ViewWordFilesTests
{
    [Test]
    public void ViewWordFiles_Should_Initialize_With_Default_Values()
    {
        // Act
        var view = new ViewWordFiles();

        // Assert
        view.Word.ShouldBe(string.Empty);
        view.WordId.ShouldBe(0);
        view.FileId.ShouldBe(Guid.Empty);
        view.IsCurrent.ShouldBeNull();
        view.IsProperName.ShouldBeNull();
    }

    [Test, AutoData]
    public void ViewWordFiles_Should_Allow_PropertyAssignment(
        string word,
        int wordId,
        Guid fileId,
        WordOrigin origin,
        bool isCurrent,
        bool isProperName)
    {
        // Act
        var view = new ViewWordFiles
        {
            Word = word,
            WordId = wordId,
            FileId = fileId,
            Origin = origin,
            IsCurrent = isCurrent,
            IsProperName = isProperName
        };

        // Assert
        view.Word.ShouldBe(word);
        view.WordId.ShouldBe(wordId);
        view.FileId.ShouldBe(fileId);
        view.Origin.ShouldBe(origin);
        view.IsCurrent.ShouldBe(isCurrent);
        view.IsProperName.ShouldBe(isProperName);
    }

    [Test]
    public void Words_Should_Support_Nullable_Boolean_Properties()
    {
        // Act
        var view = new ViewWordFiles
        {
            Word = "test",
            WordId = 1,
            FileId = Guid.NewGuid(),
            Origin = WordOrigin.Name,
            IsCurrent = null,
            IsProperName = null
        };

        // Assert
        view.IsCurrent.ShouldBeNull();
        view.IsProperName.ShouldBeNull();
    }

    [Test, AutoData]
    public void ViewWordFiles_Should_Support_All_WordOrigin_Values(
        string word,
        int wordId,
        Guid fileId)
    {
        // Act & Assert
        foreach (WordOrigin origin in Enum.GetValues(typeof(WordOrigin)))
        {
            var view = new ViewWordFiles
            {
                Word = word,
                WordId = wordId,
                FileId = fileId,
                Origin = origin
            };

            view.Origin.ShouldBe(origin);
        }
    }
}

[TestFixture]
public class WordFilesTests
{
    [Test]
    public void WordFiles_Should_Initialize_With_Default_Values()
    {
        // Act
        var wordFile = new WordFiles();

        // Assert
        wordFile.WordId.ShouldBe(0);
        wordFile.FileId.ShouldBe(Guid.Empty);
        wordFile.Origin.ShouldBe(default(WordOrigin));
    }

    [Test, AutoData]
    public void WordFiles_Should_Allow_PropertyAssignment(
        int wordId,
        Guid fileId,
        WordOrigin origin)
    {
        // Act
        var wordFile = new WordFiles
        {
            WordId = wordId,
            FileId = fileId,
            Origin = origin
        };

        // Assert
        wordFile.WordId.ShouldBe(wordId);
        wordFile.FileId.ShouldBe(fileId);
        wordFile.Origin.ShouldBe(origin);
    }

    [Test]
    public void WordFiles_Should_Support_All_WordOrigin_Values()
    {
        // Act & Assert
        foreach (WordOrigin origin in Enum.GetValues(typeof(WordOrigin)))
        {
            var wordFile = new WordFiles
            {
                WordId = 1,
                FileId = Guid.NewGuid(),
                Origin = origin
            };

            wordFile.Origin.ShouldBe(origin);
        }
    }

    [Test, AutoData]
    public void WordFiles_Should_Link_Word_And_File(int wordId, Guid fileId)
    {
        // Act
        var wordFile = new WordFiles
        {
            WordId = wordId,
            FileId = fileId,
            Origin = WordOrigin.FromTitle
        };

        // Assert
        wordFile.WordId.ShouldBe(wordId);
        wordFile.FileId.ShouldBe(fileId);
    }
}

[TestFixture]
public class WordsTests
{
    [Test]
    public void Words_Should_Initialize_With_Default_Values()
    {
        // Act
        var words = new Words();

        // Assert
        words.Id.ShouldBe(0);
        words.Word.ShouldBe(string.Empty);
        words.IsProperName.ShouldBeFalse();
        words.InsertedOn.ShouldBe(default(DateTimeOffset));
        words.UpdatedOn.ShouldBeNull();
        words.CameFromFileId.ShouldBe(Guid.Empty);
    }

    [Test, AutoData]
    public void This_Should_Allow_PropertyAssignment(
        int id,
        string word,
        WordOrigin origin,
        bool isProperName,
        DateTimeOffset insertedOn,
        DateTimeOffset updatedOn,
        Guid fileId)
    {
        // Act
        var words = new Words
        {
            Id = id,
            Word = word,
            Origin = origin,
            IsProperName = isProperName,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn,
            CameFromFileId = fileId
        };

        // Assert
        words.Id.ShouldBe(id);
        words.Word.ShouldBe(word);
        words.Origin.ShouldBe(origin);
        words.IsProperName.ShouldBe(isProperName);
        words.InsertedOn.ShouldBe(insertedOn);
        words.UpdatedOn.ShouldBe(updatedOn);
        words.CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void ViewWordFiles_Should_Support_Nullable_UpdatedOn(
        int id,
        string word,
        Guid fileId)
    {
        // Act
        var words = new Words
        {
            Id = id,
            Word = word,
            Origin = WordOrigin.Name,
            CameFromFileId = fileId,
            UpdatedOn = null
        };

        // Assert
        words.UpdatedOn.ShouldBeNull();
    }

    [Test, AutoData]
    public void Words_Should_Support_All_WordOrigin_Values(
        int id,
        string word,
        Guid fileId)
    {
        // Act & Assert
        foreach (WordOrigin origin in Enum.GetValues(typeof(WordOrigin)))
        {
            var words = new Words
            {
                Id = id,
                Word = word,
                Origin = origin,
                CameFromFileId = fileId
            };

            words.Origin.ShouldBe(origin);
        }
    }

    [Test]
    public void ViewWordFiles_Should_Track_Timestamps()
    {
        // Arrange
        var insertedOn = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedOn = DateTimeOffset.UtcNow;

        // Act
        var words = new Words
        {
            Id = 1,
            Word = "test",
            Origin = WordOrigin.Name,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn,
            CameFromFileId = Guid.NewGuid()
        };

        // Assert
        words.InsertedOn.ShouldBe(insertedOn);
        words.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ViewWordFiles_Should_Reference_Source_File(Guid fileId)
    {
        // Act
        var words = new Words
        {
            Id = 1,
            Word = "example",
            Origin = WordOrigin.Name,
            CameFromFileId = fileId
        };

        // Assert
        words.CameFromFileId.ShouldBe(fileId);
        words.CameFromFileId.ShouldNotBe(Guid.Empty);
    }
}

