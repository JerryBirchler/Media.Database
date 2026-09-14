using AutoFixture.NUnit3;
using Media.Database.Mappers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapGroupSourceMachineResponseTests
{
    [Test, AutoData]
    public void ToGroupSourceMachine_Should_Map_AllFields(
        int groupSourceMachineId, Guid groupSourceMachineUuid, int groupId, int sourceMachineId,
        bool isActive, DateTimeOffset insertedOn, DateTimeOffset updatedOn)
    {
        var mapper = new MapGroupSourceMachineResponse();

        var result = mapper.ToGroupSourceMachine(groupSourceMachineId, groupSourceMachineUuid, groupId, sourceMachineId, isActive, insertedOn, updatedOn);

        result.GroupSourceMachineId.ShouldBe(groupSourceMachineId);
        result.GroupSourceMachineUuid.ShouldBe(groupSourceMachineUuid);
        result.GroupId.ShouldBe(groupId);
        result.SourceMachineId.ShouldBe(sourceMachineId);
        result.IsActive.ShouldBe(isActive);
        result.InsertedOn.ShouldBe(insertedOn);
        result.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ToGroupSourceMachine_Should_Allow_Null_UpdatedOn(
        int groupSourceMachineId, Guid groupSourceMachineUuid, int groupId, int sourceMachineId,
        bool isActive, DateTimeOffset insertedOn)
    {
        var mapper = new MapGroupSourceMachineResponse();

        var result = mapper.ToGroupSourceMachine(groupSourceMachineId, groupSourceMachineUuid, groupId, sourceMachineId, isActive, insertedOn, updatedOn: null);

        result.UpdatedOn.ShouldBeNull();
    }
}
