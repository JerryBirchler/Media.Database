#nullable enable
using AutoFixture;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class PersonAvatarRepositoryTests
{
    private IFixture _fixture = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _cqlExecutorMock = _fixture.Freeze<Mock<ICqlQueryExecutor>>();
    }

    private PersonAvatarRepository CreateRepository() => _fixture.Create<PersonAvatarRepository>();

    private Dictionary<string, object> CaptureParameters(string cql, Func<PersonAvatarRepository, Task> act)
    {
        Action<Dictionary<string, object>>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(cql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) => captured = configure);

        act(CreateRepository()).GetAwaiter().GetResult();

        var parameters = new Dictionary<string, object>();
        captured!(parameters);
        return parameters;
    }

    [Test]
    public void PersonAvatarRepository_Should_Implement_IPersonAvatarRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IPersonAvatarRepository>();
    }

    [Test]
    public void SaveAsync_Should_ConfigureEveryColumn()
    {
        var avatar = _fixture.Create<PersonAvatar>();

        var parameters = CaptureParameters(QueryPersonAvatars.UpsertSql, r => r.SaveAsync(avatar));

        parameters[pn.PersonId.ToUpperInvariant()].ShouldBe(avatar.PersonId);
        parameters[pn.ContentType.ToUpperInvariant()].ShouldBe(avatar.ContentType);
        parameters[pn.Image.ToUpperInvariant()].ShouldBe(avatar.Image);
        parameters[pn.UpdatedOn.ToUpperInvariant()].ShouldBe(avatar.UpdatedOn);
    }

    [Test]
    public void DeleteAsync_Should_ConfigureThePersonId()
    {
        var personId = _fixture.Create<int>();

        var parameters = CaptureParameters(QueryPersonAvatars.DeleteSql, r => r.DeleteAsync(personId));

        parameters[pn.PersonId.ToUpperInvariant()].ShouldBe(personId);
    }

    [Test]
    public async Task GetAsync_Should_ReturnWhatTheExecutorReturns()
    {
        var expected = _fixture.Create<PersonAvatar>();
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonAvatars.GetSql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, PersonAvatar>>()))
            .ReturnsAsync(expected);

        (await CreateRepository().GetAsync(expected.PersonId)).ShouldBe(expected);
    }

    [Test]
    public async Task GetAsync_Should_ReturnNull_When_ThePersonHasNoPicture()
    {
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryPersonAvatars.GetSql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, PersonAvatar>>()))
            .ReturnsAsync((PersonAvatar?)null);

        (await CreateRepository().GetAsync(_fixture.Create<int>())).ShouldBeNull();
    }

    [Test]
    public void SaveAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SaveAsync(_fixture.Create<PersonAvatar>()));
    }

    [Test]
    public void DeleteAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().DeleteAsync(_fixture.Create<int>()));
    }
}
