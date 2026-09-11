using AutoFixture.NUnit3;
using Media.Database.Mappers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapPersonResponseTests
{
    [Test, AutoData]
    public void ToPerson_Should_Map_AllFields(
        int personId, Guid personUuid, string emailAddress, string cellPhoneNumber,
        string firstName, string lastName, bool isActive, DateTimeOffset insertedOn, DateTimeOffset updatedOn)
    {
        var mapper = new MapPersonResponse();

        var result = mapper.ToPerson(personId, personUuid, emailAddress, cellPhoneNumber, firstName, lastName, isActive, insertedOn, updatedOn);

        result.PersonId.ShouldBe(personId);
        result.PersonUuid.ShouldBe(personUuid);
        result.EmailAddress.ShouldBe(emailAddress);
        result.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        result.FirstName.ShouldBe(firstName);
        result.LastName.ShouldBe(lastName);
        result.IsActive.ShouldBe(isActive);
        result.InsertedOn.ShouldBe(insertedOn);
        result.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ToPerson_Should_Allow_Null_UpdatedOn(
        int personId, Guid personUuid, string emailAddress, string cellPhoneNumber,
        string firstName, string lastName, bool isActive, DateTimeOffset insertedOn)
    {
        var mapper = new MapPersonResponse();

        var result = mapper.ToPerson(personId, personUuid, emailAddress, cellPhoneNumber, firstName, lastName, isActive, insertedOn, updatedOn: null);

        result.UpdatedOn.ShouldBeNull();
    }
}
