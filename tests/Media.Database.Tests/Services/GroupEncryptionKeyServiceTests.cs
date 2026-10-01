using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Media.Database.Services;
using Media.Database.Tests.TestHelpers;
using Media.Database.Models;
using Media.Database.Repositories;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Security.Cryptography;

namespace Media.Database.Tests.Services;

[TestFixture]
public class GroupEncryptionKeyServiceTests
{
    private IFixture _fixture = null!;
    private Mock<IGroupShellRepository> _groupShellRepositoryMock = null!;
    private Mock<IGroupEncryptionKeyRepository> _groupEncryptionKeyRepositoryMock = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _groupShellRepositoryMock = _fixture.Freeze<Mock<IGroupShellRepository>>();
        _groupEncryptionKeyRepositoryMock = _fixture.Freeze<Mock<IGroupEncryptionKeyRepository>>();
    }

    private GroupEncryptionKeyService CreateService() => _fixture.Create<GroupEncryptionKeyService>();

    [Test]
    public async Task CreateShellWithDefaultKeyAsync_Should_ReturnTheCreatedShell()
    {
        var shell = _fixture.Create<GroupShell>();
        _groupShellRepositoryMock.Setup(r => r.CreateAsync()).ReturnsAsync(shell);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()))
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var (resultShell, _) = await CreateService().CreateShellWithDefaultKeyAsync();

        resultShell.ShouldBe(shell);
    }

    [Test]
    public async Task CreateShellWithDefaultKeyAsync_Should_ReturnAKeyMatchingTheGeneratorsShape()
    {
        var shell = _fixture.Create<GroupShell>();
        _groupShellRepositoryMock.Setup(r => r.CreateAsync()).ReturnsAsync(shell);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()))
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var (_, rawKey) = await CreateService().CreateShellWithDefaultKeyAsync();

        rawKey.Length.ShouldBe(26);
        rawKey.ShouldMatch("^[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{6}$");
    }

    [Test]
    public async Task CreateShellWithDefaultKeyAsync_Should_CreateAnUncoditionalUuidOrchestrationDek_ForTheNewShell()
    {
        var shell = _fixture.Create<GroupShell>();
        _groupShellRepositoryMock.Setup(r => r.CreateAsync()).ReturnsAsync(shell);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()))
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        await CreateService().CreateShellWithDefaultKeyAsync();

        _groupEncryptionKeyRepositoryMock.Verify(
            r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task CreateShellWithDefaultKeyAsync_Should_NeverPersistTheRawKey()
    {
        var shell = _fixture.Create<GroupShell>();
        string? persistedWrappedDek = null;
        _groupShellRepositoryMock.Setup(r => r.CreateAsync()).ReturnsAsync(shell);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()))
            .Callback<int, EncryptionDataCategory, string>((_, _, wrappedDek) => persistedWrappedDek = wrappedDek)
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var (_, rawKey) = await CreateService().CreateShellWithDefaultKeyAsync();

        persistedWrappedDek.ShouldNotBeNull();
        persistedWrappedDek.ShouldNotContain(rawKey);
    }

    [Test]
    public async Task UnwrapDekAsync_Should_ReturnNull_When_NoKeyExistsYet()
    {
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.GetActiveAsync(3, EncryptionDataCategory.PiiMetadata))
            .ReturnsAsync((GroupEncryptionKey?)null);

        var result = await CreateService().UnwrapDekAsync(3, EncryptionDataCategory.PiiMetadata, "any-key");

        result.ShouldBeNull();
    }

    [Test]
    public async Task UnwrapDekAsync_Should_ReturnTheOriginalDek_When_TheSameRawKeyIsSupplied()
    {
        var shell = _fixture.Create<GroupShell>();
        string? wrappedDek = null;
        _groupShellRepositoryMock.Setup(r => r.CreateAsync()).ReturnsAsync(shell);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()))
            .Callback<int, EncryptionDataCategory, string>((_, _, dek) => wrappedDek = dek)
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var service = CreateService();
        var (_, rawKey) = await service.CreateShellWithDefaultKeyAsync();

        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.GetActiveAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration))
            .ReturnsAsync(_fixture.Build<GroupEncryptionKey>().With(k => k.WrappedDek, wrappedDek!).Create());

        var unwrapped = await service.UnwrapDekAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, rawKey);

        unwrapped.ShouldNotBeNull();
        unwrapped!.Length.ShouldBe(32);
    }

    [Test]
    public async Task UnwrapDekAsync_Should_ThrowCryptographicException_When_TheRawKeyIsWrong()
    {
        var shell = _fixture.Create<GroupShell>();
        string? wrappedDek = null;
        _groupShellRepositoryMock.Setup(r => r.CreateAsync()).ReturnsAsync(shell);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, It.IsAny<string>()))
            .Callback<int, EncryptionDataCategory, string>((_, _, dek) => wrappedDek = dek)
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var service = CreateService();
        await service.CreateShellWithDefaultKeyAsync();

        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.GetActiveAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration))
            .ReturnsAsync(_fixture.Build<GroupEncryptionKey>().With(k => k.WrappedDek, wrappedDek!).Create());

        await Should.ThrowAsync<CryptographicException>(() =>
            service.UnwrapDekAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, "definitely-the-wrong-key"));
    }

    [Test]
    public async Task ResolvePiiFieldEncryptionKeyAsync_Should_CreateTheDek_When_NoneExistsYet()
    {
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.GetActiveAsync(3, EncryptionDataCategory.PiiMetadata))
            .ReturnsAsync((GroupEncryptionKey?)null);
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(3, EncryptionDataCategory.PiiMetadata, It.IsAny<string>()))
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var result = await CreateService().ResolvePiiFieldEncryptionKeyAsync(3, "raw-group-key");

        result.ShouldNotBeNullOrWhiteSpace();
        _groupEncryptionKeyRepositoryMock.Verify(r => r.CreateAsync(3, EncryptionDataCategory.PiiMetadata, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ResolvePiiFieldEncryptionKeyAsync_Should_ReturnTheSameKey_OnRepeatedCalls_When_ADekAlreadyExists()
    {
        string? wrappedDek = null;
        _groupEncryptionKeyRepositoryMock
            .SetupSequence(r => r.GetActiveAsync(3, EncryptionDataCategory.PiiMetadata))
            .ReturnsAsync((GroupEncryptionKey?)null)
            .ReturnsAsync(() => _fixture.Build<GroupEncryptionKey>().With(k => k.WrappedDek, wrappedDek!).Create());
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.CreateAsync(3, EncryptionDataCategory.PiiMetadata, It.IsAny<string>()))
            .Callback<int, EncryptionDataCategory, string>((_, _, dek) => wrappedDek = dek)
            .ReturnsAsync(_fixture.Create<GroupEncryptionKey>());

        var service = CreateService();
        var first = await service.ResolvePiiFieldEncryptionKeyAsync(3, "raw-group-key");
        var second = await service.ResolvePiiFieldEncryptionKeyAsync(3, "raw-group-key");

        second.ShouldBe(first);
        _groupEncryptionKeyRepositoryMock.Verify(r => r.CreateAsync(3, EncryptionDataCategory.PiiMetadata, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ResolvePiiFieldEncryptionKeyAsync_Should_ThrowCryptographicException_When_AnExistingDeksRawKeyIsWrong()
    {
        var validCiphertext = Media.Common.Serialization.Encryptor.Encrypt("some-dek", "the-correct-key");
        var key = _fixture.Build<GroupEncryptionKey>().With(k => k.WrappedDek, validCiphertext).Create();
        _groupEncryptionKeyRepositoryMock
            .Setup(r => r.GetActiveAsync(3, EncryptionDataCategory.PiiMetadata))
            .ReturnsAsync(key);

        await Should.ThrowAsync<CryptographicException>(() => CreateService().ResolvePiiFieldEncryptionKeyAsync(3, "wrong-key"));
    }
}
