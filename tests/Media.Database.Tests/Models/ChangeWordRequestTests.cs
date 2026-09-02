using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class ChangeWordRequestTests
{
    [Test, AutoData]
    public void ChangeWordRequest_Should_Allow_PropertyAssignment(
        string newSpan,
        WordOrigin origin,
        Guid fileId)
    {
        // Act
        var request = new ChangeWordRequest
        {
            NewSpan = newSpan,
            Origin = origin,
            CameFromFileId = fileId,
            Action = WordProducerActions.Update
        };

        // Assert
        request.NewSpan.ShouldBe(newSpan);
        request.Origin.ShouldBe(origin);
        request.CameFromFileId.ShouldBe(fileId);
        request.Action.ShouldBe(WordProducerActions.Update);
    }

    [Test]
    public void ChangeWordRequest_Should_Have_Null_CurrentSpan_By_Default()
    {
        // Act
        var request = new ChangeWordRequest
        {
            NewSpan = "new",
            Origin = WordOrigin.Name,
            CameFromFileId = Guid.NewGuid()
        };

        // Assert
        request.CurrentSpan.ShouldBeNull();
    }

    [Test, AutoData]
    public void ChangeWordRequest_Should_Allow_Setting_CurrentSpan(
        string newSpan,
        string currentSpan,
        WordOrigin origin,
        Guid fileId)
    {
        // Act
        var request = new ChangeWordRequest
        {
            NewSpan = newSpan,
            CurrentSpan = currentSpan,
            Origin = origin,
            CameFromFileId = fileId
        };

        // Assert
        request.NewSpan.ShouldBe(newSpan);
        request.CurrentSpan.ShouldBe(currentSpan);
    }

    [Test]
    public void ChangeWordRequest_Should_Support_All_WordOrigin_Values()
    {
        // Act & Assert
        foreach (WordOrigin origin in Enum.GetValues(typeof(WordOrigin)))
        {
            var request = new ChangeWordRequest
            {
                NewSpan = "test",
                Origin = origin,
                CameFromFileId = Guid.NewGuid()
            };

            request.Origin.ShouldBe(origin);
        }
    }

    [Test]
    public void ChangeWordRequest_Should_Support_All_WordProducerActions()
    {
        // Act & Assert
        foreach (WordProducerActions action in Enum.GetValues(typeof(WordProducerActions)))
        {
            var request = new ChangeWordRequest
            {
                NewSpan = "test",
                Origin = WordOrigin.Name,
                CameFromFileId = Guid.NewGuid(),
                Action = action
            };

            request.Action.ShouldBe(action);
        }
    }
}
