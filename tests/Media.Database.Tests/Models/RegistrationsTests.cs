using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class RegistrationsTests
{
    [Test]
    public void Registrations_Should_Have_DefaultValues()
    {
        // Act
        var registration = new Registrations
        {
            SourceMachineId = 0,
            EmailAddress = string.Empty,
            OtpEmail = string.Empty,
            CellPhoneNumber = string.Empty,
            OtpCellPhone = string.Empty,
            IsEmailVerified = false,
            IsSmsVerified = false
        };

        // Assert
        registration.Id.ShouldBe(0);
        registration.IsCurrent.ShouldBeFalse();
        registration.InsertedOn.ShouldBe(default(DateTimeOffset));
        registration.UpdatedOn.ShouldBeNull();
    }

    [Test, AutoData]
    public void Registrations_Should_Allow_Property_Assignment(
        int id,
        int sourceMachineId,
        string emailAddress,
        string otpEmail,
        string cellPhoneNumber,
        string otpCellPhone,
        bool isEmailVerified,
        bool isSmsVerified,
        bool isCurrent,
        DateTimeOffset insertedOn,
        DateTimeOffset updatedOn)
    {
        // Act
        var registration = new Registrations
        {
            Id = id,
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = isEmailVerified,
            IsSmsVerified = isSmsVerified,
            IsCurrent = isCurrent,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };

        // Assert
        registration.Id.ShouldBe(id);
        registration.SourceMachineId.ShouldBe(sourceMachineId);
        registration.EmailAddress.ShouldBe(emailAddress);
        registration.OtpEmail.ShouldBe(otpEmail);
        registration.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        registration.OtpCellPhone.ShouldBe(otpCellPhone);
        registration.IsEmailVerified.ShouldBe(isEmailVerified);
        registration.IsSmsVerified.ShouldBe(isSmsVerified);
        registration.IsCurrent.ShouldBe(isCurrent);
        registration.InsertedOn.ShouldBe(insertedOn);
        registration.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void Registrations_Should_Support_Nullable_UpdatedOn(
        int sourceMachineId,
        string emailAddress,
        string otpEmail,
        string cellPhoneNumber,
        string otpCellPhone)
    {
        // Act
        var registration = new Registrations
        {
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = false,
            IsSmsVerified = false,
            UpdatedOn = null
        };

        // Assert
        registration.UpdatedOn.ShouldBeNull();
    }

    [Test, AutoData]
    public void Registrations_Should_Track_Timestamps(
        int sourceMachineId,
        string emailAddress,
        string otpEmail,
        string cellPhoneNumber,
        string otpCellPhone)
    {
        // Arrange
        var insertedOn = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedOn = DateTimeOffset.UtcNow;

        // Act
        var registration = new Registrations
        {
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = false,
            IsSmsVerified = false,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn
        };

        // Assert
        registration.InsertedOn.ShouldBe(insertedOn);
        registration.UpdatedOn.ShouldBe(updatedOn);
    }

    [Test, AutoData]
    public void Registrations_Should_Reference_Source_Machine(int sourceMachineId, string emailAddress, string otpEmail, string cellPhoneNumber, string otpCellPhone)
    {
        // Act
        var registration = new Registrations
        {
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = false,
            IsSmsVerified = false
        };

        // Assert
        registration.SourceMachineId.ShouldBe(sourceMachineId);
    }

    [Test, AutoData]
    public void Registrations_Should_Track_Verification_Flags_Independently(
        int sourceMachineId,
        string emailAddress,
        string otpEmail,
        string cellPhoneNumber,
        string otpCellPhone)
    {
        // Act
        var registration = new Registrations
        {
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = true,
            IsSmsVerified = false
        };

        // Assert
        registration.IsEmailVerified.ShouldBeTrue();
        registration.IsSmsVerified.ShouldBeFalse();
    }

    [Test, AutoData]
    public void Registrations_Should_Distinguish_Current_From_Superseded_Registration(
        int sourceMachineId,
        string emailAddress,
        string otpEmail,
        string cellPhoneNumber,
        string otpCellPhone)
    {
        // Act
        var current = new Registrations
        {
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = false,
            IsSmsVerified = false,
            IsCurrent = true
        };
        var superseded = new Registrations
        {
            SourceMachineId = sourceMachineId,
            EmailAddress = emailAddress,
            OtpEmail = otpEmail,
            CellPhoneNumber = cellPhoneNumber,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = false,
            IsSmsVerified = false,
            IsCurrent = false
        };

        // Assert
        current.IsCurrent.ShouldBeTrue();
        superseded.IsCurrent.ShouldBeFalse();
    }

    [Test, AutoData]
    public void Registrations_Should_Be_Created_By_AutoFixture(Registrations registration)
    {
        // Assert
        registration.ShouldNotBeNull();
        registration.EmailAddress.ShouldNotBeNullOrEmpty();
        registration.OtpEmail.ShouldNotBeNullOrEmpty();
    }
}
