using Moq;
using NUnit.Framework;
using Serilog.Core;
using Shouldly;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class BaseRepositoryTests
{
    [Test]
    public void This_Should_Construct_FileRepository_With_Logger_And_LevelSwitch()
    {
        var logger = Mock.Of<Microsoft.Extensions.Logging.ILogger<Media.Database.Repositories.FileRepository>>();
        var levelSwitch = new LoggingLevelSwitch();

        var repo = new Media.Database.Repositories.FileRepository(logger, levelSwitch);

        repo.ShouldNotBeNull();
    }
}
