using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Media.Common.Email;
using Media.Common.Sms;
using Media.Database.Models;
using Media.Database.Services;
using Media.Database.Tests.TestHelpers;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Services;

[TestFixture]
public class GroupKeyDeliveryServiceTests
{
    [Test]
    public async Task DeliverAsync_Should_SendBySms_When_SmsIsChosen()
    {
        var fixture = AutoMoqFixture.Create();
        var smsMock = fixture.Freeze<Mock<ISmsSender>>();
        var emailMock = fixture.Freeze<Mock<IEmailSender>>();
        var service = fixture.Create<GroupKeyDeliveryService>();

        await service.DeliverAsync(KeyDeliveryMethods.Sms, "user@example.com", "+15551234567", "the-raw-key");

        smsMock.Verify(s => s.SendAsync("+15551234567", It.Is<string>(m => m.Contains("the-raw-key")), It.IsAny<CancellationToken>()), Times.Once);
        emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeliverAsync_Should_SendByEmail_When_EmailIsChosen()
    {
        var fixture = AutoMoqFixture.Create();
        var smsMock = fixture.Freeze<Mock<ISmsSender>>();
        var emailMock = fixture.Freeze<Mock<IEmailSender>>();
        var service = fixture.Create<GroupKeyDeliveryService>();

        await service.DeliverAsync(KeyDeliveryMethods.Email, "user@example.com", "+15551234567", "the-raw-key");

        emailMock.Verify(e => e.SendAsync(
            "user@example.com",
            It.IsAny<string>(),
            It.Is<string>(b => b.Contains("the-raw-key")),
            It.Is<string>(b => b.Contains("the-raw-key")),
            It.IsAny<CancellationToken>()), Times.Once);
        smsMock.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeliverAsync_Should_Throw_When_SmsIsChosen_And_NoCellPhoneNumberIsOnFile()
    {
        var fixture = AutoMoqFixture.Create();
        fixture.Freeze<Mock<ISmsSender>>();
        var service = fixture.Create<GroupKeyDeliveryService>();

        await Should.ThrowAsync<KeyDeliveryUnavailableException>(
            () => service.DeliverAsync(KeyDeliveryMethods.Sms, "user@example.com", "   ", "the-raw-key"));
    }

    [Test]
    public async Task DeliverAsync_Should_Throw_When_EmailIsChosen_And_NoEmailAddressIsOnFile()
    {
        var fixture = AutoMoqFixture.Create();
        fixture.Freeze<Mock<IEmailSender>>();
        var service = fixture.Create<GroupKeyDeliveryService>();

        await Should.ThrowAsync<KeyDeliveryUnavailableException>(
            () => service.DeliverAsync(KeyDeliveryMethods.Email, "", "+15551234567", "the-raw-key"));
    }

    [Test]
    public async Task DeliverAsync_Should_Not_FallBackToEmail_When_TheSmsSenderFails()
    {
        var fixture = AutoMoqFixture.Create();
        var smsMock = fixture.Freeze<Mock<ISmsSender>>();
        var emailMock = fixture.Freeze<Mock<IEmailSender>>();
        smsMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("SMS dispatch is not configured on this deployment."));
        var service = fixture.Create<GroupKeyDeliveryService>();

        await Should.ThrowAsync<NotSupportedException>(
            () => service.DeliverAsync(KeyDeliveryMethods.Sms, "user@example.com", "+15551234567", "the-raw-key"));

        emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeliverAsync_Should_Throw_When_TheKeyIsBlank()
    {
        var fixture = AutoMoqFixture.Create();
        fixture.Freeze<Mock<ISmsSender>>();
        var service = fixture.Create<GroupKeyDeliveryService>();

        await Should.ThrowAsync<ArgumentException>(
            () => service.DeliverAsync(KeyDeliveryMethods.Sms, "user@example.com", "+15551234567", "   "));
    }
}
