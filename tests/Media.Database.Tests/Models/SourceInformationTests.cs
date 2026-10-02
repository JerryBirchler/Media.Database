using AutoFixture.NUnit3;
using Media.Database.Models;
using Media.Database.Tests.TestHelpers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class AddSourceInformationRequestTests
{
    // API-142: the request carries canonical values, so whatever reaches the repository is already
    // the one form of each name, address and number.
    [Test]
    public void AddSourceInformationRequest_Should_HoldCanonicalIdentityValues()
    {
        var request = new AddSourceInformationRequest
        {
            SourceMachineName = "Laptop",
            DeviceTypeId = DeviceTypes.PC,
            EmailAddress = " Jerry@Example.com ".Email(),
            CellPhoneNumber = "(214) 555-1234".Phone(),
            FirstName = "  Jerry ".Name(),
            LastName = "Birchler".Name(),
            OperatingSystem = "Windows"
        };

        request.EmailAddress.ToString().ShouldBe("jerry@example.com");
        request.CellPhoneNumber.ToString().ShouldBe("+12145551234");
        request.FirstName.ToString().ShouldBe("Jerry");
        request.LastName.ToString().ShouldBe("Birchler");
    }

    // A phone is optional at registration; until one is added it is simply absent.
    [Test]
    public void AddSourceInformationRequest_Should_AllowNoPhone()
    {
        var request = new AddSourceInformationRequest
        {
            SourceMachineName = "Laptop",
            DeviceTypeId = DeviceTypes.PC,
            EmailAddress = "jerry@example.com".Email(),
            CellPhoneNumber = null,
            FirstName = "Jerry".Name(),
            LastName = "Birchler".Name(),
            OperatingSystem = "Windows"
        };

        request.CellPhoneNumber.ShouldBeNull();
    }
}

[TestFixture]
public class UpdateSourceInformationRequestTests
{
    [Test]
    public void UpdateSourceInformationRequest_Should_HoldCanonicalContactValues()
    {
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineId = 7,
            EmailAddress = "JERRY@example.com".Email(),
            CellPhoneNumber = "1 214 555 1234".Phone(),
            OperatingSystem = "Windows"
        };

        request.SourceMachineId.ShouldBe(7);
        request.EmailAddress.ToString().ShouldBe("jerry@example.com");
        request.CellPhoneNumber.ToString().ShouldBe("+12145551234");
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
            DisambiguationKey = string.Empty,
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
        string disambiguationKey,
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
            DisambiguationKey = disambiguationKey,
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
        response.DisambiguationKey.ShouldBe(disambiguationKey);
        response.EmailAddress.ShouldBe(emailAddress);
        response.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        response.FirstName.ShouldBe(firstName);
        response.LastName.ShouldBe(lastName);
        response.OperatingSystem.ShouldBe(operatingSystem);
        response.InsertedOn.ShouldBe(insertedOn);
        response.IsActive.ShouldBe(isActive);
    }

    [Test, AutoData]
    public void SourceInformationResponse_Should_Not_Serialize_SourceMachineUuid(SourceInformationResponse response)
    {
        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(response);

        // Assert
        json.ShouldNotContain("sourceMachineUuid");
        json.ShouldNotContain("SourceMachineUuid");
    }
}
