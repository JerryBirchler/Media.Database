using AutoFixture.NUnit3;
using Media.Database.Mappers;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Mappers;

[TestFixture]
public class MapRegistrationResponsesTests
{
    [Test, AutoData]
    public void ToOtpEmailResponse_Should_PopulateApiKey_When_BothChannelsVerified(
        Guid sourceMachineUuid, string sourceMachineName, DeviceTypes deviceTypeId,
        string firstName, string lastName, string emailAddress)
    {
        var mapper = new MapRegistrationResponses();

        var result = mapper.ToOtpEmailResponse(sourceMachineUuid, sourceMachineName, deviceTypeId, firstName, lastName, emailAddress, isEmailVerified: true, isSmsVerified: true);

        result.ApiKey.ShouldBe(sourceMachineUuid);
    }

    [Test, AutoData]
    public void ToOtpEmailResponse_Should_Not_PopulateApiKey_When_SmsNotYetVerified(
        Guid sourceMachineUuid, string sourceMachineName, DeviceTypes deviceTypeId,
        string firstName, string lastName, string emailAddress)
    {
        var mapper = new MapRegistrationResponses();

        var result = mapper.ToOtpEmailResponse(sourceMachineUuid, sourceMachineName, deviceTypeId, firstName, lastName, emailAddress, isEmailVerified: true, isSmsVerified: false);

        result.ApiKey.ShouldBeNull();
    }

    [Test, AutoData]
    public void ToOtpEmailResponse_Should_Map_AllFields(
        Guid sourceMachineUuid, string sourceMachineName, DeviceTypes deviceTypeId,
        string firstName, string lastName, string emailAddress)
    {
        var mapper = new MapRegistrationResponses();

        var result = mapper.ToOtpEmailResponse(sourceMachineUuid, sourceMachineName, deviceTypeId, firstName, lastName, emailAddress, isEmailVerified: true, isSmsVerified: false);

        result.SourceMachineUuid.ShouldBe(sourceMachineUuid);
        result.SourceMachineName.ShouldBe(sourceMachineName);
        result.DeviceTypeId.ShouldBe(deviceTypeId);
        result.FirstName.ShouldBe(firstName);
        result.LastName.ShouldBe(lastName);
        result.EmailAddress.ShouldBe(emailAddress);
        result.OtpEmailVerified.ShouldBeTrue();
    }

    [Test, AutoData]
    public void ToOtpSmsResponse_Should_PopulateApiKey_When_BothChannelsVerified(
        Guid sourceMachineUuid, string sourceMachineName, DeviceTypes deviceTypeId,
        string firstName, string lastName, string cellPhoneNumber)
    {
        var mapper = new MapRegistrationResponses();

        var result = mapper.ToOtpSmsResponse(sourceMachineUuid, sourceMachineName, deviceTypeId, firstName, lastName, cellPhoneNumber, isSmsVerified: true, isEmailVerified: true);

        result.ApiKey.ShouldBe(sourceMachineUuid);
    }

    [Test, AutoData]
    public void ToOtpSmsResponse_Should_Not_PopulateApiKey_When_EmailNotYetVerified(
        Guid sourceMachineUuid, string sourceMachineName, DeviceTypes deviceTypeId,
        string firstName, string lastName, string cellPhoneNumber)
    {
        var mapper = new MapRegistrationResponses();

        var result = mapper.ToOtpSmsResponse(sourceMachineUuid, sourceMachineName, deviceTypeId, firstName, lastName, cellPhoneNumber, isSmsVerified: true, isEmailVerified: false);

        result.ApiKey.ShouldBeNull();
    }

    [Test, AutoData]
    public void ToOtpSmsResponse_Should_Map_AllFields(
        Guid sourceMachineUuid, string sourceMachineName, DeviceTypes deviceTypeId,
        string firstName, string lastName, string cellPhoneNumber)
    {
        var mapper = new MapRegistrationResponses();

        var result = mapper.ToOtpSmsResponse(sourceMachineUuid, sourceMachineName, deviceTypeId, firstName, lastName, cellPhoneNumber, isSmsVerified: true, isEmailVerified: false);

        result.SourceMachineUuid.ShouldBe(sourceMachineUuid);
        result.SourceMachineName.ShouldBe(sourceMachineName);
        result.DeviceTypeId.ShouldBe(deviceTypeId);
        result.FirstName.ShouldBe(firstName);
        result.LastName.ShouldBe(lastName);
        result.CellPhoneNumber.ShouldBe(cellPhoneNumber);
        result.OtpSmsVerified.ShouldBeTrue();
    }
}
