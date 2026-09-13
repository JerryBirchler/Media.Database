using Media.Database.Models;
using Media.Database.Repositories;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class WordRepositoryTests
{
    private static WordRepository CreateRepository()
    {
        return new WordRepository(
            Mock.Of<ISqlQueryExecutor>(),
            Mock.Of<ICqlQueryExecutor>(),
            Mock.Of<Media.Common.Providers.IScyllaSessionProvider>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WordRepository>>(),
            new Serilog.Core.LoggingLevelSwitch());
    }

    [Test]
    public void WordRepository_Constructor_Should_Accept_Logger_And_LevelSwitch()
    {
        var repo = CreateRepository();

        repo.ShouldNotBeNull();
    }

    [Test]
    public void WordRepository_Should_Implement_IWordRepository()
    {
        var repo = CreateRepository();
        repo.ShouldBeAssignableTo<IWordRepository>();
    }

    [Test]
    public void WordRepository_Should_Inherit_From_BaseRepository()
    {
        var repo = CreateRepository();
        repo.ShouldBeAssignableTo<BaseRepository>();
    }

    [Test]
    public void GetByUuid_Should_Exist_With_Correct_Signature()
    {
        var repo = CreateRepository();
        var method = repo.GetType().GetMethod("GetByUuid", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<Words>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Guid));
    }

    [Test]
    public void Upsert_Should_Exist_With_Correct_Signature()
    {
        var repo = CreateRepository();
        var method = repo.GetType().GetMethod("Upsert", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(UpsertWordRequest));
    }

    [Test]
    public void RefreshView_Should_Exist_With_Correct_Signature()
    {
        var repo = CreateRepository();
        var method = repo.GetType().GetMethod("RefreshView", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(0);
    }

    [Test]
    public void DeleteByUuid_Should_Exist_With_Correct_Signature()
    {
        var repo = CreateRepository();
        var method = repo.GetType().GetMethod("DeleteByUuid", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Guid));
    }

}

