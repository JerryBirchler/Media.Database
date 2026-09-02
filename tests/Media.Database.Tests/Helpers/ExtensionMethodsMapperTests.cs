using AutoFixture.NUnit3;
using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Media.Database.Tests.Helpers;

[TestFixture]
public class ExtensionMethodsTests
{
    private readonly IMapChangeWordRequests _changeWordMapper = new MapChangeWordRequests();

    [Test, AutoData]
    public void ProcessList_Should_Add_Delete_For_Removed_Items(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var current = new Files { Id = fileId };
        var curList = new[] { "word1", "word2", "word3" };
        var newList = new[] { "word1", "word3" }; // "word2" removed

        // Act
        updates.ProcessList(curList, newList, current, origin, _changeWordMapper);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(WordProducerActions.Delete);
        updates[0].NewSpan.ShouldBe("word2");
    }

    [Test, AutoData]
    public void ProcessList_Should_Add_Upsert_For_Added_Items(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var current = new Files { Id = fileId };
        var curList = new[] { "word1", "word2" };
        var newList = new[] { "word1", "word2", "word3" }; // "word3" added

        // Act
        updates.ProcessList(curList, newList, current, origin, _changeWordMapper);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(WordProducerActions.Upsert);
        updates[0].NewSpan.ShouldBe("word3");
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Update_When_Values_Different(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var current = new Files { Id = fileId };

        // Act
        updates.ProcessScalar("oldValue", "newValue", current, origin, _changeWordMapper);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(WordProducerActions.Update);
        updates[0].CurrentSpan.ShouldBe("oldValue");
        updates[0].NewSpan.ShouldBe("newValue");
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Delete_When_New_Value_Null(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var current = new Files { Id = fileId };

        // Act
        updates.ProcessScalar("oldValue", null, current, origin, _changeWordMapper);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(WordProducerActions.Delete);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Upsert_When_Current_Value_Null(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var current = new Files { Id = fileId };

        // Act
        updates.ProcessScalar(null, "newValue", current, origin, _changeWordMapper);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(WordProducerActions.Upsert);
        updates[0].NewSpan.ShouldBe("newValue");
    }
}

