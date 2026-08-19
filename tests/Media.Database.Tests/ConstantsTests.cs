using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests;

[TestFixture]
public class ConstantsTests
{
    [Test]
    public void NotFound_Should_Be_Valid_FormatString()
    {
        // Arrange
        var testType = "File";
        var testId = "123";
        var testLocation = "Repository";

        // Act
        var result = string.Format(Media.Database.Constants.NotFound, testType, testId, testLocation);

        // Assert
        result.ShouldBe("File 123 not found in Repository.");
    }

    [Test]
    public void NotFound_Should_Contain_Three_Placeholders()
    {
        // Arrange & Act
        var constant = Media.Database.Constants.NotFound;

        // Assert
        constant.ShouldContain("{0}");
        constant.ShouldContain("{1}");
        constant.ShouldContain("{2}");
    }
}
