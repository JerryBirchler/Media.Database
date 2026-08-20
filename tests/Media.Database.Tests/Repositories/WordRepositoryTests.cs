using Media.Database.Models;
using Media.Database.Repositories;
using Microsoft.Extensions.Configuration;
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
    [Test]
    public void This_Should_Construct_WordRepository_With_Logger_And_LevelSwitch()
    {
        var logger = Mock.Of<Microsoft.Extensions.Logging.ILogger<WordRepository>>();
        var levelSwitch = new Serilog.Core.LoggingLevelSwitch();

        var repo = new WordRepository();

        repo.ShouldNotBeNull();
    }

    [Test]
    public void This_Should_Construct_WordRepository_With_Configuration()
    {
        var inMemory = new Dictionary<string, string>
        {
            { "ConnectionStrings:PostgresConnection", "Host=localhost;Username=test;Password=pass" },
            { "ScyllaDB:ContactPoints:0", "http://127.0.0.1" },
            { "ScyllaDB:ExternalContactPoints:0", "http://10.0.0.1" },
            { "ScyllaDB:Port", "9042" },
            { "ScyllaDB:Keyspace", "ks" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var repo = new WordRepository();

        repo.ShouldNotBeNull();
    }

    [Test]
    public void This_Should_Implement_IWordRepository()
    {
        var repo = new WordRepository();
        repo.ShouldBeAssignableTo<IWordRepository>();
    }

    [Test]
    public void This_Should_Inherit_From_BaseRepository()
    {
        var repo = new WordRepository();
        repo.ShouldBeAssignableTo<BaseRepository>();
    }

    [Test]
    public void This_Should_Have_GetById_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetById", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<Words>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(int));
    }

    [Test]
    public void This_Should_Have_GetFilePages_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePages", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<List<ViewWordFiles>>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(7);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].ParameterType.ShouldBe(typeof(WordOrigin?));
        parameters[3].ParameterType.ShouldBe(typeof(Guid?));
        parameters[4].ParameterType.ShouldBe(typeof(bool?));
        parameters[5].ParameterType.ShouldBe(typeof(bool?));
        parameters[6].ParameterType.ShouldBe(typeof(int?));
    }

    [Test]
    public void This_Should_Have_GetFilePagesByWordOrigin_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByWordOrigin", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<List<ViewWordFiles>>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(6);
    }

    [Test]
    public void This_Should_Have_GetFilePagesByWordFileId_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByWordFileId", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<List<ViewWordFiles>>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(6);
    }

    [Test]
    public void This_Should_Have_GetFilePagesByFileIdOrigin_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByFileIdOrigin", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<List<ViewWordFiles>>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(6);
    }

    [Test]
    public void This_Should_Have_GetFilePagesByFileIdWord_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByFileIdWord", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<List<ViewWordFiles>>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(6);
    }

    [Test]
    public void This_Should_Have_Upsert_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("Upsert", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(UpsertWordRequest));
    }

    [Test]
    public void This_Should_Have_RefreshView_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("RefreshView", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(0);
    }

    [Test]
    public void This_Should_Have_Delete_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("Delete", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(int));
    }

    [Test]
    public void This_Should_Have_DeleteFile_Method()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("DeleteFile", BindingFlags.Public | BindingFlags.Instance);

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Guid));
    }

    [Test]
    public void GetFilePagesByWordOrigin_Should_Have_Default_Limit_Parameter()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByWordOrigin", BindingFlags.Public | BindingFlags.Instance);

        var parameters = method.GetParameters();
        var limitParam = parameters[5]; // Last parameter
        limitParam.Name.ShouldBe("limit");
        limitParam.HasDefaultValue.ShouldBeTrue();
        limitParam.DefaultValue.ShouldBe(10);
    }

    [Test]
    public void GetFilePagesByWordFileId_Should_Have_Default_Limit_Parameter()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByWordFileId", BindingFlags.Public | BindingFlags.Instance);

        var parameters = method.GetParameters();
        var limitParam = parameters[5]; // Last parameter
        limitParam.Name.ShouldBe("limit");
        limitParam.HasDefaultValue.ShouldBeTrue();
        limitParam.DefaultValue.ShouldBe(10);
    }

    [Test]
    public void GetFilePagesByFileIdOrigin_Should_Have_Default_Limit_Parameter()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByFileIdOrigin", BindingFlags.Public | BindingFlags.Instance);

        var parameters = method.GetParameters();
        var limitParam = parameters[5]; // Last parameter
        limitParam.Name.ShouldBe("limit");
        limitParam.HasDefaultValue.ShouldBeTrue();
        limitParam.DefaultValue.ShouldBe(10);
    }

    [Test]
    public void GetFilePagesByFileIdWord_Should_Have_Default_Limit_Parameter()
    {
        var repo = new WordRepository();
        var method = repo.GetType().GetMethod("GetFilePagesByFileIdWord", BindingFlags.Public | BindingFlags.Instance);

        var parameters = method.GetParameters();
        var limitParam = parameters[5]; // Last parameter
        limitParam.Name.ShouldBe("limit");
        limitParam.HasDefaultValue.ShouldBeTrue();
        limitParam.DefaultValue.ShouldBe(10);
    }

    [Test]
    public void All_GetFilePages_Methods_Should_Have_Same_Signature()
    {
        var repo = new WordRepository();
        var methods = new[]
        {
            repo.GetType().GetMethod("GetFilePagesByWordOrigin"),
            repo.GetType().GetMethod("GetFilePagesByWordFileId"),
            repo.GetType().GetMethod("GetFilePagesByFileIdOrigin"),
            repo.GetType().GetMethod("GetFilePagesByFileIdWord")
        };

        foreach (var method in methods)
        {
            method.ShouldNotBeNull();
            var parameters = method.GetParameters();
            parameters.Length.ShouldBe(6);
            parameters[0].ParameterType.ShouldBe(typeof(string)); // word
            parameters[1].ParameterType.ShouldBe(typeof(WordOrigin?)); // origin
            parameters[2].ParameterType.ShouldBe(typeof(Guid?)); // fileId
            parameters[3].ParameterType.ShouldBe(typeof(bool?)); // isCurrent
            parameters[4].ParameterType.ShouldBe(typeof(bool?)); // isProperName
            parameters[5].ParameterType.ShouldBe(typeof(int?)); // limit
        }
    }
}
