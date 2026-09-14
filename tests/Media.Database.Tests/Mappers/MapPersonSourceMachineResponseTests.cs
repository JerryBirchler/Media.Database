using AutoFixture.NUnit3;
using Media.Database.Mappers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapPersonSourceMachineResponseTests
{
    [Test, AutoData]
    public void ToPersonSourceMachine_Should_Map_AllFields(
        int personSourceMachineId, Guid personSourceMachineUuid, int personId, int sourceMachineId,
        bool isActive, DateTimeOffset insertedOn, DateTimeOffset updatedOn)
    {
        var mapper = new MapPersonSourceMachineResponse();

        var result = mapper.ToPersonSourceMachine(personSourceMachineId, personSourceMachineUuid, personId, sourceMachineId, isActive, insertedOn, updatedOn);

        result.PersonSourceMachineId.ShouldBe(personSourceMachineId);
        result.PersonSourceMachineUuid.ShouldBe(personSourceMachineUuid);
        result.PersonId.ShouldBe(personId);
        result.SourceMachineId.ShouldBe(sourceMachineId);
        result.IsActive.ShouldBe(isActive);
        result.InsertedOn.ShouldBe(insertedOn);
        result.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ToPersonSourceMachine_Should_Allow_Null_UpdatedOn(
        int personSourceMachineId, Guid personSourceMachineUuid, int personId, int sourceMachineId,
        bool isActive, DateTimeOffset insertedOn)
    {
        var mapper = new MapPersonSourceMachineResponse();

        var result = mapper.ToPersonSourceMachine(personSourceMachineId, personSourceMachineUuid, personId, sourceMachineId, isActive, insertedOn, updatedOn: null);

        result.UpdatedOn.ShouldBeNull();
    }
}
