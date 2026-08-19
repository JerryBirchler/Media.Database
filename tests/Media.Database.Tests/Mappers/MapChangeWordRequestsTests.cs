using AutoFixture.NUnit3;
using Media.Database.Mappers;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapChangeWordRequestsTests
{
    [Test, AutoData]
    public void ProcessList_Should_Add_Delete_For_Removed_Items(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var curList = new[] { "word1", "word2", "word3" };
        var newList = new[] { "word1", "word3" }; // "word2" removed

        // Act
        mapper.ProcessList(updates, curList, newList, current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Delete);
        updates[0].NewSpan.ShouldBe("word2");
        updates[0].Origin.ShouldBe(origin);
        updates[0].CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void ProcessList_Should_Add_Upsert_For_Added_Items(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var curList = new[] { "word1", "word2" };
        var newList = new[] { "word1", "word2", "word3" }; // "word3" added

        // Act
        mapper.ProcessList(updates, curList, newList, current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Upsert);
        updates[0].NewSpan.ShouldBe("word3");
        updates[0].Origin.ShouldBe(origin);
        updates[0].CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void ProcessList_Should_Handle_Both_Additions_And_Deletions(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var curList = new[] { "word1", "word2", "word3" };
        var newList = new[] { "word1", "word4", "word5" }; // removed: word2, word3; added: word4, word5

        // Act
        mapper.ProcessList(updates, curList, newList, current, origin);

        // Assert
        updates.Count.ShouldBe(4);
        var deletes = updates.Where(u => u.Action == KafkaProducerActions.Delete).ToList();
        var upserts = updates.Where(u => u.Action == KafkaProducerActions.Upsert).ToList();

        deletes.Count.ShouldBe(2);
        deletes.Select(d => d.NewSpan).ShouldBe(new[] { "word2", "word3" }, ignoreOrder: true);

        upserts.Count.ShouldBe(2);
        upserts.Select(u => u.NewSpan).ShouldBe(new[] { "word4", "word5" }, ignoreOrder: true);
    }

    [Test, AutoData]
    public void ProcessList_Should_Handle_Null_Current_List(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var newList = new[] { "word1", "word2" };

        // Act
        mapper.ProcessList(updates, null, newList, current, origin);

        // Assert
        updates.Count.ShouldBe(2);
        updates.All(u => u.Action == KafkaProducerActions.Upsert).ShouldBeTrue();
        updates.Select(u => u.NewSpan).ShouldBe(newList);
    }

    [Test, AutoData]
    public void ProcessList_Should_Handle_Null_New_List(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var curList = new[] { "word1", "word2" };

        // Act
        mapper.ProcessList(updates, curList, null, current, origin);

        // Assert
        updates.Count.ShouldBe(2);
        updates.All(u => u.Action == KafkaProducerActions.Delete).ShouldBeTrue();
        updates.Select(u => u.NewSpan).ShouldBe(curList);
    }

    [Test, AutoData]
    public void ProcessList_Should_Handle_Both_Lists_Null(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessList(updates, null, null, current, origin);

        // Assert
        updates.Count.ShouldBe(0);
    }

    [Test, AutoData]
    public void ProcessList_Should_Handle_Empty_Lists(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var emptyList = Enumerable.Empty<string>();

        // Act
        mapper.ProcessList(updates, emptyList, emptyList, current, origin);

        // Assert
        updates.Count.ShouldBe(0);
    }

    [Test, AutoData]
    public void ProcessList_Should_Handle_Identical_Lists(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };
        var list = new[] { "word1", "word2", "word3" };

        // Act
        mapper.ProcessList(updates, list, list, current, origin);

        // Assert
        updates.Count.ShouldBe(0);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Update_When_Both_Values_Present_And_Different(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "oldValue", "newValue", current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Update);
        updates[0].CurrentSpan.ShouldBe("oldValue");
        updates[0].NewSpan.ShouldBe("newValue");
        updates[0].Origin.ShouldBe(origin);
        updates[0].CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Not_Add_Update_When_Values_Are_Same(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "sameValue", "sameValue", current, origin);

        // Assert
        updates.Count.ShouldBe(0);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Delete_When_New_Value_Is_Null(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "oldValue", null, current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Delete);
        updates[0].CurrentSpan.ShouldBe("oldValue");
        updates[0].Origin.ShouldBe(origin);
        updates[0].CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Delete_When_New_Value_Is_Empty(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "oldValue", "", current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Delete);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Delete_When_New_Value_Is_Whitespace(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "oldValue", "   ", current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Delete);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Upsert_When_Current_Value_Is_Null(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, null, "newValue", current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Upsert);
        updates[0].NewSpan.ShouldBe("newValue");
        updates[0].Origin.ShouldBe(origin);
        updates[0].CameFromFileId.ShouldBe(fileId);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Upsert_When_Current_Value_Is_Empty(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "", "newValue", current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Upsert);
        updates[0].NewSpan.ShouldBe("newValue");
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Add_Upsert_When_Current_Value_Is_Whitespace(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "   ", "newValue", current, origin);

        // Assert
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Upsert);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Not_Add_When_Both_Values_Empty(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, null, null, current, origin);

        // Assert
        updates.Count.ShouldBe(0);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Not_Add_When_Both_Values_Whitespace(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "  ", "   ", current, origin);

        // Assert
        updates.Count.ShouldBe(0);
    }

    [Test, AutoData]
    public void ProcessScalar_Should_Be_Case_Sensitive(WordOrigin origin, Guid fileId)
    {
        // Arrange
        var updates = new List<ChangeWordRequest>();
        var mapper = new MapChangeWordRequests();
        var current = new Files { Id = fileId };

        // Act
        mapper.ProcessScalar(updates, "value", "Value", current, origin);

        // Assert - Different case should trigger update
        updates.Count.ShouldBe(1);
        updates[0].Action.ShouldBe(KafkaProducerActions.Update);
    }
}
