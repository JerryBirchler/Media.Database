using AutoFixture.NUnit3;
using Media.Database.Mappers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapGroupPersonResponseTests
{
    [Test, AutoData]
    public void ToGroupPerson_Should_Map_AllFields(
        int groupPersonId, Guid groupPersonUuid, int groupId, int personId,
        bool isActive, bool isAdmin, DateTimeOffset insertedOn, DateTimeOffset updatedOn)
    {
        var mapper = new MapGroupPersonResponse();

        var result = mapper.ToGroupPerson(groupPersonId, groupPersonUuid, groupId, personId, isActive, isAdmin, insertedOn, updatedOn);

        result.GroupPersonId.ShouldBe(groupPersonId);
        result.GroupPersonUuid.ShouldBe(groupPersonUuid);
        result.GroupId.ShouldBe(groupId);
        result.PersonId.ShouldBe(personId);
        result.IsActive.ShouldBe(isActive);
        result.IsAdmin.ShouldBe(isAdmin);
        result.InsertedOn.ShouldBe(insertedOn);
        result.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ToGroupPerson_Should_Allow_Null_UpdatedOn(
        int groupPersonId, Guid groupPersonUuid, int groupId, int personId,
        bool isActive, bool isAdmin, DateTimeOffset insertedOn)
    {
        var mapper = new MapGroupPersonResponse();

        var result = mapper.ToGroupPerson(groupPersonId, groupPersonUuid, groupId, personId, isActive, isAdmin, insertedOn, updatedOn: null);

        result.UpdatedOn.ShouldBeNull();
    }
}
