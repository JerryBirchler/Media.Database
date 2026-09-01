using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class AddSourceInformationRequestTests
{
    [Test]
    public void AddSourceInformationRequest_Should_Have_DefaultValues()
    {
        // Act
        var request = new AddSourceInformationRequest
        {
            SourceMachineName = string.Empty,
            DeviceTypeId = default,
            EmailAddress = string.Empty,
            CellPhoneNumber = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            OperatingSystem = string.Empty
        };

        // Assert
        request.SourceMachineName.ShouldBe(string.Empty);
        request.EmailAddress.ShouldBe(string.Empty);
        request.CellPhoneNumber.ShouldBe(string.Empty);
        request.FirstName.ShouldBe(string.Empty);
        request.LastName.ShouldBe(string.Empty);
        request.OperatingSystem.ShouldBe(string.Empty);
    }

    [Test, AutoData]
    public void AddSourceInformationRequest_Should_Allow_Property_Assignment(
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string emailAddress,
        string cellPhoneNumber,
        string firstName,
        string lastName,
        string operatingSystem)
    {
        // Act
        var request = new AddSourceInformationRequest
        {
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            EmailAddress = emailAddress,
            CellPhoneNumber = cellPhoneNumber,
            FirstName = firstName,
            LastName = lastName,
            OperatingSystem = operatingSystem
        };

        // Assert
        request.SourceMachineName.ShouldBe(sourceMachineName);
        request.DeviceTypeId.ShouldBe(deviceTypeId);
        request.EmailAddress.ShouldBe(emailAddress);
        request.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        request.FirstName.ShouldBe(firstName);
        request.LastName.ShouldBe(lastName);
        request.OperatingSystem.ShouldBe(operatingSystem);
    }
}

[TestFixture]
public class UpdateSourceInformationRequestTests
{
    [Test]
    public void UpdateSourceInformationRequest_Should_Have_DefaultValues()
    {
        // Act
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = Guid.Empty,
            EmailAddress = string.Empty,
            CellPhoneNumber = string.Empty,
            OperatingSystem = string.Empty
        };

        // Assert
        request.SourceMachineUuid.ShouldBe(Guid.Empty);
        request.EmailAddress.ShouldBe(string.Empty);
        request.CellPhoneNumber.ShouldBe(string.Empty);
        request.OperatingSystem.ShouldBe(string.Empty);
    }

    [Test, AutoData]
    public void UpdateSourceInformationRequest_Should_Allow_Property_Assignment(
        Guid sourceMachineUuid,
        string emailAddress,
        string cellPhoneNumber,
        string operatingSystem)
    {
        // Act
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = sourceMachineUuid,
            EmailAddress = emailAddress,
            CellPhoneNumber = cellPhoneNumber,
            OperatingSystem = operatingSystem
        };

        // Assert
        request.SourceMachineUuid.ShouldBe(sourceMachineUuid);
        request.EmailAddress.ShouldBe(emailAddress);
        request.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        request.OperatingSystem.ShouldBe(operatingSystem);
    }
}

[TestFixture]
public class SourceInformationResponseTests
{
    [Test]
    public void SourceInformationResponse_Should_Have_DefaultValues()
    {
        // Act
        var response = new SourceInformationResponse
        {
            SourceMachineUuid = Guid.Empty,
            SourceMachineName = string.Empty,
            DeviceTypeId = default,
            EmailAddress = string.Empty,
            CellPhoneNumber = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            OperatingSystem = string.Empty,
            InsertedOn = default,
            IsActive = false
        };

        // Assert
        response.SourceMachineUuid.ShouldBe(Guid.Empty);
        response.IsActive.ShouldBeFalse();
        response.InsertedOn.ShouldBe(default(DateTimeOffset));
    }

    [Test, AutoData]
    public void SourceInformationResponse_Should_Allow_Property_Assignment(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string emailAddress,
        string cellPhoneNumber,
        string firstName,
        string lastName,
        string operatingSystem,
        DateTimeOffset insertedOn,
        bool isActive)
    {
        // Act
        var response = new SourceInformationResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            EmailAddress = emailAddress,
            CellPhoneNumber = cellPhoneNumber,
            FirstName = firstName,
            LastName = lastName,
            OperatingSystem = operatingSystem,
            InsertedOn = insertedOn,
            IsActive = isActive
        };

        // Assert
        response.SourceMachineUuid.ShouldBe(sourceMachineUuid);
        response.SourceMachineName.ShouldBe(sourceMachineName);
        response.DeviceTypeId.ShouldBe(deviceTypeId);
        response.EmailAddress.ShouldBe(emailAddress);
        response.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        response.FirstName.ShouldBe(firstName);
        response.LastName.ShouldBe(lastName);
        response.OperatingSystem.ShouldBe(operatingSystem);
        response.InsertedOn.ShouldBe(insertedOn);
        response.IsActive.ShouldBe(isActive);
    }
}
