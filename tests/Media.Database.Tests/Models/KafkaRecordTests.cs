using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class KafkaRecordTests
{
    [Test, AutoData]
    public void This_Should_Be_Created_With_All_Properties(
        string topic,
        int partition,
        long offset,
        CreateWordRequest value)
    {
        // Act
        var record = new KafkaRecord(topic, partition, offset, value);

        // Assert
        record.Topic.ShouldBe(topic);
        record.Partition.ShouldBe(partition);
        record.Offset.ShouldBe(offset);
        record.Value.ShouldBe(value);
    }

    [Test]
    public void This_Should_Be_A_Record_Type()
    {
        // Arrange
        var value = new CreateWordRequest
        {
            Word = "test",
            Origin = WordOrigin.Name,
            IsProperName = false,
            CameFromFileId = Guid.NewGuid()
        };

        var record1 = new KafkaRecord("topic1", 0, 100, value);
        var record2 = new KafkaRecord("topic1", 0, 100, value);

        // Assert - Records with same values should be equal
        record1.ShouldBe(record2);
    }

    [Test, AutoData]
    public void This_Should_Support_Deconstruction(
        string topic,
        int partition,
        long offset,
        CreateWordRequest value)
    {
        // Arrange
        var record = new KafkaRecord(topic, partition, offset, value);

        // Act
        var (t, p, o, v) = record;

        // Assert
        t.ShouldBe(topic);
        p.ShouldBe(partition);
        o.ShouldBe(offset);
        v.ShouldBe(value);
    }

    [Test]
    public void This_Should_Store_Partition_And_Offset()
    {
        // Arrange
        var value = new CreateWordRequest
        {
            Word = "test",
            Origin = WordOrigin.Name,
            IsProperName = false,
            CameFromFileId = Guid.NewGuid()
        };

        // Act
        var record = new KafkaRecord("test-topic", 5, 1234567890L, value);

        // Assert
        record.Partition.ShouldBe(5);
        record.Offset.ShouldBe(1234567890L);
    }
}

[TestFixture]
public class KafkaRecordWrapperTests
{
    [Test, AutoData]
    public void This_Should_Be_Created_With_Value(BaseWordRequest value)
    {
        // Act
        var wrapper = new KafkaRecordWrapper(value);

        // Assert
        wrapper.Value.ShouldBe(value);
    }

    [Test]
    public void This_Should_Be_A_Record_Type()
    {
        // Arrange
        var value = new BaseWordRequest
        {
            Word = "test",
            Origin = WordOrigin.Name,
            IsProperName = false,
            CameFromFileId = Guid.NewGuid()
        };

        var wrapper1 = new KafkaRecordWrapper(value);
        var wrapper2 = new KafkaRecordWrapper(value);

        // Assert
        wrapper1.ShouldBe(wrapper2);
    }

    [Test]
    public void This_Should_Wrap_BaseWordRequest()
    {
        // Arrange
        var baseRequest = new BaseWordRequest
        {
            Word = "example",
            Origin = WordOrigin.FromTitle,
            IsProperName = true,
            CameFromFileId = Guid.NewGuid()
        };

        // Act
        var wrapper = new KafkaRecordWrapper(baseRequest);

        // Assert
        wrapper.Value.ShouldBeOfType<BaseWordRequest>();
        wrapper.Value.Word.ShouldBe("example");
    }

    [Test]
    public void This_Should_Wrap_DeleteWordRequest()
    {
        // Arrange
        var deleteRequest = new DeleteWordRequest
        {
            Word = "remove",
            Origin = WordOrigin.Name,
            IsProperName = false,
            CameFromFileId = Guid.NewGuid()
        };

        // Act
        var wrapper = new KafkaRecordWrapper(deleteRequest);

        // Assert
        wrapper.Value.ShouldBeOfType<DeleteWordRequest>();
        wrapper.Value.Action.ShouldBe(KafkaProducerActions.Delete);
    }
}

[TestFixture]
public class ChangeWordRequestTests
{
    [Test, AutoData]
    public void This_Should_Allow_PropertyAssignment(
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
            Action = KafkaProducerActions.Update
        };

        // Assert
        request.NewSpan.ShouldBe(newSpan);
        request.Origin.ShouldBe(origin);
        request.CameFromFileId.ShouldBe(fileId);
        request.Action.ShouldBe(KafkaProducerActions.Update);
    }

    [Test]
    public void This_Should_Have_Null_CurrentSpan_By_Default()
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
    public void This_Should_Allow_Setting_CurrentSpan(
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
    public void This_Should_Support_All_WordOrigin_Values()
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
    public void This_Should_Support_All_KafkaProducerActions()
    {
        // Act & Assert
        foreach (KafkaProducerActions action in Enum.GetValues(typeof(KafkaProducerActions)))
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

