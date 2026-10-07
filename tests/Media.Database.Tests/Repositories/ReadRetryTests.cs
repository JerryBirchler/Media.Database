#nullable enable
using Media.Database.Repositories;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers ReadRetry: a read whose connection failed is tried again; a write, or a failure of the
/// statement rather than the connection, never is. No waiting: the delays here are zero.
/// </summary>
[TestFixture]
public class ReadRetryTests
{
    private static readonly ReadRetry TwoMore = new([TimeSpan.Zero, TimeSpan.Zero]);
    private static bool IsIo(Exception exception) => exception is IOException;

    [TestCase("SELECT * FROM files")]
    [TestCase("  \n select id from files")]
    public void IsRead_Should_BeTrue_ForAPlainSelect(string statement)
    {
        ReadRetry.IsRead(statement).ShouldBeTrue();
    }

    [TestCase("INSERT INTO files VALUES (1)")]
    [TestCase("UPDATE files SET x = 1")]
    [TestCase("DELETE FROM files")]
    [TestCase("WITH moved AS (DELETE FROM files RETURNING *) SELECT * FROM moved")]
    public void IsRead_Should_BeFalse_ForAnythingThatCanWrite(string statement)
    {
        ReadRetry.IsRead(statement).ShouldBeFalse();
    }

    [Test]
    public async Task RunAsync_Should_TryAReadAgain_When_ItsConnectionFailed()
    {
        var attempts = 0;
        var cleared = 0;

        var result = await TwoMore.RunAsync("SELECT 1", () =>
        {
            attempts++;
            return attempts == 1 ? throw new IOException("reset") : Task.FromResult("row");
        }, IsIo, () => cleared++);

        result.ShouldBe("row");
        attempts.ShouldBe(2);
        cleared.ShouldBe(1);
    }

    [Test]
    public async Task RunAsync_Should_SurfaceTheLastFailure_When_EveryAttemptFails()
    {
        var attempts = 0;

        var failure = await Should.ThrowAsync<IOException>(() => TwoMore.RunAsync<string>("SELECT 1", () =>
        {
            attempts++;
            throw new IOException($"reset {attempts}");
        }, IsIo));

        failure.Message.ShouldBe("reset 3");
        attempts.ShouldBe(3);
    }

    [Test]
    public async Task RunAsync_Should_NeverRepeatAWrite_When_ItsConnectionFailed()
    {
        var attempts = 0;

        await Should.ThrowAsync<IOException>(() => TwoMore.RunAsync<int>("INSERT INTO files VALUES (1)", () =>
        {
            attempts++;
            throw new IOException("reset");
        }, IsIo));

        attempts.ShouldBe(1);
    }

    [Test]
    public async Task RunAsync_Should_FailAtOnce_When_TheStatementItselfFailed()
    {
        var attempts = 0;

        await Should.ThrowAsync<InvalidOperationException>(() => TwoMore.RunAsync<string>("SELECT 1", () =>
        {
            attempts++;
            throw new InvalidOperationException("syntax error");
        }, IsIo));

        attempts.ShouldBe(1);
    }

    [Test]
    public void Default_Should_TryThreeMoreTimes_OverAboutTwoAndAHalfSeconds()
    {
        ReadRetry.Default.Delays.Count.ShouldBe(3);
        ReadRetry.Default.Delays.Aggregate(TimeSpan.Zero, (sum, delay) => sum + delay).ShouldBe(TimeSpan.FromMilliseconds(2500));
    }
}
