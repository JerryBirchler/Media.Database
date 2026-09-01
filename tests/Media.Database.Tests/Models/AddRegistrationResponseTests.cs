using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class AddRegistrationResponseTests
{
    [Test]
    public void AddRegistrationResponse_Should_Have_DefaultValues()
    {
        // Act
        var response = new AddRegistrationResponse
        {
            Id = 0,
            OtpEmail = string.Empty,
            OtpCellPhone = string.Empty,
            IsEmailVerified = false,
            IsSmsVerified = false,
            InsertedOn = default,
            UpdatedOn = null
        };

        // Assert
        response.Id.ShouldBe(0);
        response.IsEmailVerified.ShouldBeFalse();
        response.IsSmsVerified.ShouldBeFalse();
        response.UpdatedOn.ShouldBeNull();
    }

    [Test, AutoData]
    public void AddRegistrationResponse_Should_Allow_Property_Assignment(
        int id,
        string otpEmail,
        string otpCellPhone,
        bool isEmailVerified,
        bool isSmsVerified,
        DateTimeOffset insertedOn,
        DateTimeOffset updatedOn)
    {
        // Act
        var response = new AddRegistrationResponse
        {
            Id = id,
            OtpEmail = otpEmail,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = isEmailVerified,
            IsSmsVerified = isSmsVerified,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };

        // Assert
        response.Id.ShouldBe(id);
        response.OtpEmail.ShouldBe(otpEmail);
        response.OtpCellPhone.ShouldBe(otpCellPhone);
        response.IsEmailVerified.ShouldBe(isEmailVerified);
        response.IsSmsVerified.ShouldBe(isSmsVerified);
        response.InsertedOn.ShouldBe(insertedOn);
        response.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void AddRegistrationResponse_Should_Support_Nullable_UpdatedOn(int id, string otpEmail, string otpCellPhone)
    {
        // Act
        var response = new AddRegistrationResponse
        {
            Id = id,
            OtpEmail = otpEmail,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = false,
            IsSmsVerified = false,
            InsertedOn = DateTimeOffset.UtcNow,
            UpdatedOn = null
        };

        // Assert
        response.UpdatedOn.ShouldBeNull();
    }
}
