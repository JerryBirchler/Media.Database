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
        string firstName, string lastName, bool isActive, int createdByPersonId, bool isSuperAdmin,
        bool isEmailVerified, bool isSmsVerified, int otpWindowOverrideMinutes,
        DateTimeOffset insertedOn, DateTimeOffset updatedOn)
    {
        var mapper = new MapPersonResponse();

        var result = mapper.ToPerson(
            personId, personUuid, emailAddress, cellPhoneNumber, firstName, lastName, isActive,
            createdByPersonId, isSuperAdmin, isEmailVerified, isSmsVerified, otpWindowOverrideMinutes,
            insertedOn, updatedOn);

        result.PersonId.ShouldBe(personId);
        result.PersonUuid.ShouldBe(personUuid);
        result.EmailAddress.ShouldBe(emailAddress);
        result.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        result.FirstName.ShouldBe(firstName);
        result.LastName.ShouldBe(lastName);
        result.IsActive.ShouldBe(isActive);
        result.CreatedByPersonId.ShouldBe(createdByPersonId);
        result.IsSuperAdmin.ShouldBe(isSuperAdmin);
        result.IsEmailVerified.ShouldBe(isEmailVerified);
        result.IsSmsVerified.ShouldBe(isSmsVerified);
        result.OtpWindowOverrideMinutes.ShouldBe(otpWindowOverrideMinutes);
        result.InsertedOn.ShouldBe(insertedOn);
        result.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void ToPerson_Should_Allow_Null_UpdatedOn(
        int personId, Guid personUuid, string emailAddress, string cellPhoneNumber,
        string firstName, string lastName, bool isActive, bool isSuperAdmin,
        bool isEmailVerified, bool isSmsVerified, DateTimeOffset insertedOn)
    {
        var mapper = new MapPersonResponse();

        var result = mapper.ToPerson(
            personId, personUuid, emailAddress, cellPhoneNumber, firstName, lastName, isActive,
            createdByPersonId: null, isSuperAdmin, isEmailVerified, isSmsVerified,
            otpWindowOverrideMinutes: null, insertedOn, updatedOn: null);

        result.CreatedByPersonId.ShouldBeNull();
        result.OtpWindowOverrideMinutes.ShouldBeNull();
        result.UpdatedOn.ShouldBeNull();
    }
}
