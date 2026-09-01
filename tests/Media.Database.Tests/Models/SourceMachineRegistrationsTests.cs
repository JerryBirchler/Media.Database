using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class SourceMachineRegistrationsTests
{
    private static SourceMachineRegistrations CreateMinimal(
        DeviceTypes deviceTypeId = DeviceTypes.PC,
        bool hasRegistration = false,
        DateTimeOffset? updatedOn = null,
        DateTimeOffset? registrationInsertedOn = null,
        DateTimeOffset? registrationUpdatedOn = null)
    {
        return new SourceMachineRegistrations
        {
            RegistrationId = 0,
            SourceMachineId = 0,
            SourceMachineUuid = Guid.Empty,
            SourceMachineName = string.Empty,
            DeviceTypeId = deviceTypeId,
            OperatingSystem = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            EmailAddress = string.Empty,
            CellPhoneNumber = string.Empty,
            HasRegistration = hasRegistration,
            IsEmailVerified = false,
            IsSmsVerified = false,
            InsertedOn = default,
            UpdatedOn = updatedOn,
            IsActive = false,
            OtpEmail = string.Empty,
            OtpCellPhone = string.Empty,
            RegistrationInsertedOn = registrationInsertedOn,
            RegistrationUpdatedOn = registrationUpdatedOn
        };
    }

    [Test]
    public void SourceMachineRegistrations_Should_Have_DefaultValues()
    {
        // Act
        var registration = CreateMinimal();

        // Assert
        registration.SourceMachineId.ShouldBe(0);
        registration.SourceMachineUuid.ShouldBe(Guid.Empty);
        registration.HasRegistration.ShouldBeFalse();
        registration.IsActive.ShouldBeFalse();
        registration.InsertedOn.ShouldBe(default(DateTimeOffset));
        registration.UpdatedOn.ShouldBeNull();
        registration.RegistrationInsertedOn.ShouldBeNull();
        registration.RegistrationUpdatedOn.ShouldBeNull();
    }

    [Test, AutoData]
    public void SourceMachineRegistrations_Should_Allow_Property_Assignment(
        int registrationId,
        int sourceMachineId,
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string operatingSystem,
        string firstName,
        string lastName,
        string emailAddress,
        string cellPhoneNumber,
        bool hasRegistration,
        bool isEmailVerified,
        bool isSmsVerified,
        bool isActive,
        DateTimeOffset insertedOn,
        DateTimeOffset updatedOn,
        string otpEmail,
        string otpCellPhone,
        DateTimeOffset registrationInsertedOn,
        DateTimeOffset registrationUpdatedOn)
    {
        // Act
        var registration = new SourceMachineRegistrations
        {
            RegistrationId = registrationId,
            SourceMachineId = sourceMachineId,
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            OperatingSystem = operatingSystem,
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = emailAddress,
            CellPhoneNumber = cellPhoneNumber,
            HasRegistration = hasRegistration,
            IsEmailVerified = isEmailVerified,
            IsSmsVerified = isSmsVerified,
            InsertedOn = insertedOn,
            UpdatedOn = updatedOn,
            IsActive = isActive,
            OtpEmail = otpEmail,
            OtpCellPhone = otpCellPhone,
            RegistrationInsertedOn = registrationInsertedOn,
            RegistrationUpdatedOn = registrationUpdatedOn
        };

        // Assert
        registration.RegistrationId.ShouldBe(registrationId);
        registration.SourceMachineId.ShouldBe(sourceMachineId);
        registration.SourceMachineUuid.ShouldBe(sourceMachineUuid);
        registration.SourceMachineName.ShouldBe(sourceMachineName);
        registration.DeviceTypeId.ShouldBe(deviceTypeId);
        registration.OperatingSystem.ShouldBe(operatingSystem);
        registration.FirstName.ShouldBe(firstName);
        registration.LastName.ShouldBe(lastName);
        registration.EmailAddress.ShouldBe(emailAddress);
        registration.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        registration.HasRegistration.ShouldBe(hasRegistration);
        registration.IsEmailVerified.ShouldBe(isEmailVerified);
        registration.IsSmsVerified.ShouldBe(isSmsVerified);
        registration.InsertedOn.ShouldBe(insertedOn);
        registration.UpdatedOn.ShouldBe(updatedOn);
        registration.IsActive.ShouldBe(isActive);
        registration.OtpEmail.ShouldBe(otpEmail);
        registration.OtpCellPhone.ShouldBe(otpCellPhone);
        registration.RegistrationInsertedOn.ShouldBe(registrationInsertedOn);
        registration.RegistrationUpdatedOn.ShouldBe(registrationUpdatedOn);
    }

    [Test]
    public void SourceMachineRegistrations_Should_Support_Nullable_UpdatedOn_And_RegistrationTimestamps()
    {
        // Act
        var registration = CreateMinimal(updatedOn: null, registrationInsertedOn: null, registrationUpdatedOn: null);

        // Assert
        registration.UpdatedOn.ShouldBeNull();
        registration.RegistrationInsertedOn.ShouldBeNull();
        registration.RegistrationUpdatedOn.ShouldBeNull();
    }

    [Test]
    public void SourceMachineRegistrations_Should_Support_All_DeviceTypes_Values()
    {
        // Act & Assert
        foreach (DeviceTypes deviceType in Enum.GetValues(typeof(DeviceTypes)))
        {
            var registration = CreateMinimal(deviceTypeId: deviceType);

            registration.DeviceTypeId.ShouldBe(deviceType);
        }
    }

    [Test]
    public void SourceMachineRegistrations_Should_Track_HasRegistration_Flag()
    {
        // Act
        var withRegistration = CreateMinimal(hasRegistration: true);
        var withoutRegistration = CreateMinimal(hasRegistration: false);

        // Assert
        withRegistration.HasRegistration.ShouldBeTrue();
        withoutRegistration.HasRegistration.ShouldBeFalse();
    }

    [Test, AutoData]
    public void SourceMachineRegistrations_Should_Track_Verification_Flags_Independently(string otpEmail, string otpCellPhone)
    {
        // Act
        var registration = CreateMinimal() with
        {
            OtpEmail = otpEmail,
            OtpCellPhone = otpCellPhone,
            IsEmailVerified = true,
            IsSmsVerified = false
        };

        // Assert
        registration.IsEmailVerified.ShouldBeTrue();
        registration.IsSmsVerified.ShouldBeFalse();
    }

    [Test]
    public void SourceMachineRegistrations_Should_Support_With_Expression_For_Registration_Mutation()
    {
        // Arrange
        var original = CreateMinimal();

        // Act
        var mutated = original with
        {
            RegistrationId = 42,
            OtpEmail = "123456",
            OtpCellPhone = "654321",
            RegistrationInsertedOn = DateTimeOffset.UtcNow
        };

        // Assert
        mutated.RegistrationId.ShouldBe(42);
        mutated.OtpEmail.ShouldBe("123456");
        mutated.OtpCellPhone.ShouldBe("654321");
        mutated.RegistrationInsertedOn.ShouldNotBeNull();
        original.RegistrationId.ShouldBe(0);
    }

    [Test, AutoData]
    public void SourceMachineRegistrations_Should_Be_Created_By_AutoFixture(SourceMachineRegistrations registration)
    {
        // Assert
        registration.ShouldNotBeNull();
        registration.SourceMachineName.ShouldNotBeNullOrEmpty();
        registration.SourceMachineUuid.ShouldNotBe(Guid.Empty);
    }
}
