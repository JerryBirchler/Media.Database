using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class BaseOtpVerificationResponseTests
{
    [Test]
    public void BaseOtpVerificationResponse_Should_Have_DefaultValues()
    {
        // Act
        var response = new BaseOtpVerificationResponse
        {
            SourceMachineUuid = Guid.Empty,
            SourceMachineName = string.Empty,
            DeviceTypeId = default,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        // Assert
        response.SourceMachineUuid.ShouldBe(Guid.Empty);
        response.SourceMachineName.ShouldBe(string.Empty);
        response.FirstName.ShouldBe(string.Empty);
        response.LastName.ShouldBe(string.Empty);
    }

    [Test, AutoData]
    public void BaseOtpVerificationResponse_Should_Allow_Property_Assignment(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName)
    {
        // Act
        var response = new BaseOtpVerificationResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName
        };

        // Assert
        response.SourceMachineUuid.ShouldBe(sourceMachineUuid);
        response.SourceMachineName.ShouldBe(sourceMachineName);
        response.DeviceTypeId.ShouldBe(deviceTypeId);
        response.FirstName.ShouldBe(firstName);
        response.LastName.ShouldBe(lastName);
    }
}

[TestFixture]
public class OtpEmailResponseTests
{
    [Test, AutoData]
    public void OtpEmailResponse_Should_Inherit_From_BaseOtpVerificationResponse(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string emailAddress,
        bool otpEmailVerified)
    {
        // Act
        var response = new OtpEmailResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = emailAddress,
            OtpEmailVerified = otpEmailVerified
        };

        // Assert
        response.ShouldBeAssignableTo<BaseOtpVerificationResponse>();
    }

    [Test, AutoData]
    public void OtpEmailResponse_Should_Allow_Property_Assignment(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string emailAddress,
        bool otpEmailVerified)
    {
        // Act
        var response = new OtpEmailResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = emailAddress,
            OtpEmailVerified = otpEmailVerified
        };

        // Assert
        response.EmailAddress.ShouldBe(emailAddress);
        response.OtpEmailVerified.ShouldBe(otpEmailVerified);
    }

    [Test]
    public void OtpEmailResponse_Should_Default_OtpEmailVerified_To_False()
    {
        // Act
        var response = new OtpEmailResponse
        {
            SourceMachineUuid = Guid.NewGuid(),
            SourceMachineName = "machine",
            DeviceTypeId = DeviceTypes.Phone,
            FirstName = "Alice",
            LastName = "Smith",
            EmailAddress = "alice@example.com",
            OtpEmailVerified = false
        };

        // Assert
        response.OtpEmailVerified.ShouldBeFalse();
    }
}

[TestFixture]
public class OtpSmsResponseTests
{
    [Test, AutoData]
    public void OtpSmsResponse_Should_Inherit_From_BaseOtpVerificationResponse(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string cellPhoneNumber,
        bool otpSmsVerified)
    {
        // Act
        var response = new OtpSmsResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName,
            CellPhoneNumber = cellPhoneNumber,
            OtpSmsVerified = otpSmsVerified
        };

        // Assert
        response.ShouldBeAssignableTo<BaseOtpVerificationResponse>();
    }

    [Test, AutoData]
    public void OtpSmsResponse_Should_Allow_Property_Assignment(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string cellPhoneNumber,
        bool otpSmsVerified)
    {
        // Act
        var response = new OtpSmsResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName,
            CellPhoneNumber = cellPhoneNumber,
            OtpSmsVerified = otpSmsVerified
        };

        // Assert
        response.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        response.OtpSmsVerified.ShouldBe(otpSmsVerified);
    }

    [Test]
    public void OtpSmsResponse_Should_Default_OtpSmsVerified_To_False()
    {
        // Act
        var response = new OtpSmsResponse
        {
            SourceMachineUuid = Guid.NewGuid(),
            SourceMachineName = "machine",
            DeviceTypeId = DeviceTypes.Tablet,
            FirstName = "Alice",
            LastName = "Smith",
            CellPhoneNumber = "555-0100",
            OtpSmsVerified = false
        };

        // Assert
        response.OtpSmsVerified.ShouldBeFalse();
    }
}
