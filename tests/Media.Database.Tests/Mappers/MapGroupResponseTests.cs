using AutoFixture.NUnit3;
using Media.Database.Mappers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapGroupResponseTests
{
    [Test, AutoData]
    public void ToGroup_Should_Map_AllFields(
        int groupId, Guid groupUuid, string name, string title, string description,
        bool isActive, DateTimeOffset insertedOn, DateTimeOffset updatedOn)
    {
        var mapper = new MapGroupResponse();

        var result = mapper.ToGroup(groupId, groupUuid, name, title, description, isActive, insertedOn, updatedOn);

        result.GroupId.ShouldBe(groupId);
        result.GroupUuid.ShouldBe(groupUuid);
        result.Name.ShouldBe(name);
        result.Title.ShouldBe(title);
        result.Description.ShouldBe(description);
        result.IsActive.ShouldBe(isActive);
        result.InsertedOn.ShouldBe(insertedOn);
        result.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ToGroup_Should_Allow_Null_Description_And_UpdatedOn(
        int groupId, Guid groupUuid, string name, string title, bool isActive, DateTimeOffset insertedOn)
    {
        var mapper = new MapGroupResponse();

        var result = mapper.ToGroup(groupId, groupUuid, name, title, description: null, isActive, insertedOn, updatedOn: null);

        result.Description.ShouldBeNull();
        result.UpdatedOn.ShouldBeNull();
    }
}
