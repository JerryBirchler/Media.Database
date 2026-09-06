using Media.Common.Transactions;
using Media.Database.Mappers;
using Media.Database.Repositories;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class FileRepositoryTests
{
    [Test]
    public void FileRepository_Constructor_Should_Accept_Dependencies()
    {
        var repo = new FileRepository(
            Mock.Of<ISqlQueryExecutor>(),
            () => Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapChangeWordRequests>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<FileRepository>>(),
            new Serilog.Core.LoggingLevelSwitch());

        repo.ShouldNotBeNull();
    }
}
