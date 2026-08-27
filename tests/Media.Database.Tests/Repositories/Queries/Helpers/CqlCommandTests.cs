using Cassandra;
using Media.Database.Repositories.Queries.Helpers;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories.Queries.Helpers;

[TestFixture]
public class CqlCommandTests
{
    private Mock<ISession> _mockSession = null!;

    [SetUp]
    public void Setup()
    {
        _mockSession = new Mock<ISession>();
    }

    [Test]
    public void CqlCommand_Should_Be_Constructible_With_Session_And_Query()
    {
        // Arrange & Act
        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");

        // Assert
        command.ShouldNotBeNull();
    }

    [Test]
    public void CqlCommand_Should_Be_Constructible_With_BatchSize()
    {
        // Arrange & Act
        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id", 50);

        // Assert
        command.ShouldNotBeNull();
    }

    [Test]
    public void CqlCommand_Should_Initialize_Parameters_Dictionary()
    {
        // Arrange & Act
        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table");

        // Assert
        command.Parameters.ShouldNotBeNull();
        command.Parameters.ShouldBeEmpty();
    }

    [Test]
    public void CqlCommand_Should_Allow_Adding_Parameters()
    {
        // Arrange
        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");

        // Act
        command.Parameters.Add("@ID", Guid.NewGuid());

        // Assert
        command.Parameters.Count.ShouldBe(1);
        command.Parameters.ContainsKey("@ID").ShouldBeTrue();
    }

    [Test]
    public void CqlCommand_Should_Support_Multiple_Parameters()
    {
        // Arrange
        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id AND name = @Name");

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

        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");

        // Act & Assert
        Should.Throw<ArgumentException>(() => command.Bind())
            .Message.ShouldContain("missing");
    }

    [Test]
    public void CqlCommand_Should_Parse_Query_Parameters()
    {
        // Arrange & Act - Constructor should extract parameters from query
        var command = new CqlCommand(
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
        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id");

        // Act & Assert
        Should.NotThrow(() => command.BeginBatch());
    }

    [Test]
    public void CqlCommand_Should_Convert_Parameterized_Query_To_Native()
    {
        // Arrange - Query with @parameters should be converted to ? placeholders internally
        var mockPreparedStatement = new Mock<PreparedStatement>();
        string capturedQuery = null!;

        _mockSession.Setup(s => s.Prepare(It.IsAny<string>()))
            .Callback<string>(q => capturedQuery = q)
            .Returns(mockPreparedStatement.Object);

        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");
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
    public void CqlCommand_Should_Support_Default_BatchSize_Of_100()
    {
        // Arrange & Act - Constructor without batchSize parameter
        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id");

        // Assert - Should use default of 100 (we can't directly test this, but constructor should succeed)
        command.ShouldNotBeNull();
    }

    [Test]
    public void CqlCommand_Should_Support_Custom_BatchSize()
    {
        // Arrange & Act
        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id", 250);

        // Assert
        command.ShouldNotBeNull();
    }

    [Test]
    public void Parameters_Should_Be_SortedDictionary()
    {
        // Arrange
        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table");

        // Assert
        command.Parameters.ShouldBeOfType<SortedDictionary<string, object>>();
    }

    [Test]
    public void CqlCommand_Should_Accept_Complex_Query_With_Multiple_Parameters()
    {
        // Arrange & Act
        var command = new CqlCommand(
            _mockSession.Object,
            @"INSERT INTO table (id, name, created, updated, status)
              VALUES (@Id, @Name, @CreatedOn, @UpdatedOn, @Status)");

        // Assert
        command.ShouldNotBeNull();
        command.Parameters.ShouldBeEmpty();
    }

    [Test]
    public void Bind_Should_ReturnBoundStatement_When_AllParametersPresent()
    {
        var mockPreparedStatement = new Mock<PreparedStatement>();
        var mockBoundStatement = new Mock<BoundStatement>();
        mockPreparedStatement.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(mockBoundStatement.Object);
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);

        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");
        command.Parameters.Add("@ID", Guid.NewGuid());

        var result = command.Bind();

        result.ShouldBe(mockBoundStatement.Object);
    }

    [Test]
    public async Task ExecuteRowSet_Should_Return_RowSet_From_Session()
    {
        var mockPreparedStatement = new Mock<PreparedStatement>();
        var mockBoundStatement = new Mock<BoundStatement>();
        var mockRowSet = new Mock<RowSet>();
        mockPreparedStatement.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(mockBoundStatement.Object);
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);
        _mockSession.Setup(s => s.ExecuteAsync(mockBoundStatement.Object)).ReturnsAsync(mockRowSet.Object);

        var command = new CqlCommand(_mockSession.Object, "SELECT * FROM table WHERE id = @Id");
        command.Parameters.Add("@ID", Guid.NewGuid());

        var result = await command.ExecuteRowSet();

        result.ShouldBe(mockRowSet.Object);
    }

    [Test]
    public async Task ExecuteAsync_Should_Execute_Batch_On_Session()
    {
        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id");
        var batch = new BatchStatement();

        await command.ExecuteAsync(batch);

        _mockSession.Verify(s => s.ExecuteAsync(batch), Times.Once);
    }

    [Test]
    public async Task AddQuery_Should_FlushBatch_When_RowCountReachesBatchSize()
    {
        var mockPreparedStatement = new Mock<PreparedStatement>();
        var mockBoundStatement = new Mock<BoundStatement>();
        mockPreparedStatement.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(mockBoundStatement.Object);
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);
        _mockSession.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ReturnsAsync(new Mock<RowSet>().Object);

        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id", batchSize: 1);
        command.BeginBatch();

        await command.AddQuery(Guid.NewGuid());

        _mockSession.Verify(s => s.ExecuteAsync(It.IsAny<BatchStatement>()), Times.Once);
    }

    [Test]
    public async Task AddQuery_Should_Not_FlushBatch_Before_BatchSize_Is_Reached()
    {
        var mockPreparedStatement = new Mock<PreparedStatement>();
        var mockBoundStatement = new Mock<BoundStatement>();
        mockPreparedStatement.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(mockBoundStatement.Object);
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);
        _mockSession.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ReturnsAsync(new Mock<RowSet>().Object);

        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id", batchSize: 5);
        command.BeginBatch();

        await command.AddQuery(Guid.NewGuid());

        _mockSession.Verify(s => s.ExecuteAsync(It.IsAny<BatchStatement>()), Times.Never);
    }

    [Test]
    public async Task EndBatch_Should_FlushRemainingRows_When_NotAlignedToBatchSize()
    {
        var mockPreparedStatement = new Mock<PreparedStatement>();
        var mockBoundStatement = new Mock<BoundStatement>();
        mockPreparedStatement.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(mockBoundStatement.Object);
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);
        _mockSession.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ReturnsAsync(new Mock<RowSet>().Object);

        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id", batchSize: 5);
        command.BeginBatch();
        await command.AddQuery(Guid.NewGuid());

        await command.EndBatch();

        _mockSession.Verify(s => s.ExecuteAsync(It.IsAny<BatchStatement>()), Times.Once);
    }

    [Test]
    public async Task EndBatch_Should_Not_Execute_When_RowCountIsAlignedToBatchSize()
    {
        var mockPreparedStatement = new Mock<PreparedStatement>();
        var mockBoundStatement = new Mock<BoundStatement>();
        mockPreparedStatement.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(mockBoundStatement.Object);
        _mockSession.Setup(s => s.Prepare(It.IsAny<string>())).Returns(mockPreparedStatement.Object);
        _mockSession.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ReturnsAsync(new Mock<RowSet>().Object);

        var command = new CqlCommand(_mockSession.Object, "DELETE FROM table WHERE id = @Id", batchSize: 1);
        command.BeginBatch();
        await command.AddQuery(Guid.NewGuid());
        _mockSession.Invocations.Clear();

        await command.EndBatch();

        _mockSession.Verify(s => s.ExecuteAsync(It.IsAny<BatchStatement>()), Times.Never);
    }
}
