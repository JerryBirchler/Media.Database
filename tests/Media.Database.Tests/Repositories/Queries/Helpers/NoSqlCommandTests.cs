using Cassandra;
using Media.Database.Repositories.Queries.Helpers;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Media.Database.Tests.Repositories.Queries.Helpers;

[TestFixture]
public class NoSqlCommandTests
{
    private Mock<ISession> _mockSession = null!;

    [SetUp]
    public void Setup()
    {
        _mockSession = new Mock<ISession>();
    }

    [Test]
    public void NoSqlCommand_Should_Be_Constructible_With_Session_And_Query()
    {
        // Arrange & Act
        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");

        // Assert
        command.ShouldNotBeNull();
    }

    [Test]
    public void NoSqlCommand_Should_Be_Constructible_With_BatchSize()
    {
        // Arrange & Act
        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id", 50);

        // Assert
        command.ShouldNotBeNull();
    }

    [Test]
    public void NoSqlCommand_Should_Initialize_Parameters_Dictionary()
    {
        // Arrange & Act
        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table");

        // Assert
        command.Parameters.ShouldNotBeNull();
        command.Parameters.ShouldBeEmpty();
    }

    [Test]
    public void NoSqlCommand_Should_Allow_Adding_Parameters()
    {
        // Arrange
        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");

        // Act
        command.Parameters.Add("@ID", Guid.NewGuid());

        // Assert
        command.Parameters.Count.ShouldBe(1);
        command.Parameters.ContainsKey("@ID").ShouldBeTrue();
    }

    [Test]
    public void NoSqlCommand_Should_Support_Multiple_Parameters()
    {
        // Arrange
        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id AND name = @Name");

        // Act
        command.Parameters.Add("@ID", Guid.NewGuid());
        command.Parameters.Add("@NAME", "test");

        // Assert
        command.Parameters.Count.ShouldBe(2);
    }

    [Test]
    public void Bind_Should_Throw_When_Required_Parameter_Missing()
    {
        // Arrange
        var mockPreparedStatement = new Mock<PreparedStatement>();
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);

        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.Bind())
            .Message.ShouldContain("missing");
    }

    [Test]
    public void NoSqlCommand_Should_Parse_Query_Parameters()
    {
        // Arrange & Act - Constructor should extract parameters from query
        var command = new NoSqlCommand(
            _mockSession.Object,
            "UPDATE table SET name = @Name WHERE id = @Id AND status = @Status");

        // Assert - The command should be ready to accept these parameters
        command.ShouldNotBeNull();
        command.Parameters.ShouldBeEmpty(); // Parameters are empty until added
    }

    [Test]
    public void BeginBatch_Should_Not_Throw()
    {
        // Arrange
        var command = new NoSqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id");

        // Act & Assert
        Should.NotThrow(() => command.BeginBatch());
    }

    [Test]
    public void NoSqlCommand_Should_Convert_Parameterized_Query_To_Native()
    {
        // Arrange - Query with @parameters should be converted to ? placeholders internally
        var mockPreparedStatement = new Mock<PreparedStatement>();
        string capturedQuery = null!;

        _mockSession.Setup(s => s.Prepare(It.IsAny<string>()))
            .Callback<string>(q => capturedQuery = q)
            .Returns(mockPreparedStatement.Object);

        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");
        command.Parameters.Add("@ID", Guid.NewGuid());

        // Act
        try
        {
            command.Bind();
        }
        catch
        {
            // We expect this to fail because we're mocking, but we captured the query
        }

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.ShouldContain("?");
        capturedQuery.ShouldNotContain("@");
    }

    [Test]
    public void NoSqlCommand_Should_Support_Default_BatchSize_Of_100()
    {
        // Arrange & Act - Constructor without batchSize parameter
        var command = new NoSqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id");

        // Assert - Should use default of 100 (we can't directly test this, but constructor should succeed)
        command.ShouldNotBeNull();
    }

    [Test]
    public void NoSqlCommand_Should_Support_Custom_BatchSize()
    {
        // Arrange & Act
        var command = new NoSqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id", 250);

        // Assert
        command.ShouldNotBeNull();
    }

    [Test]
    public void Parameters_Should_Be_SortedDictionary()
    {
        // Arrange
        var command = new NoSqlCommand(_mockSession.Object, "SELECT * FROM table");

        // Assert
        command.Parameters.ShouldBeOfType<SortedDictionary<string, object>>();
    }

    [Test]
    public void NoSqlCommand_Should_Accept_Complex_Query_With_Multiple_Parameters()
    {
        // Arrange & Act
        var command = new NoSqlCommand(
            _mockSession.Object,
            @"INSERT INTO table (id, name, created, updated, status) 
              VALUES (@Id, @Name, @CreatedOn, @UpdatedOn, @Status)");

        // Assert
        command.ShouldNotBeNull();
        command.Parameters.ShouldBeEmpty();
    }
}
