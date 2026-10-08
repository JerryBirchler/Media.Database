#nullable enable
using AutoFixture;
using Media.Database.Models;
using Media.Database.Tests.TestHelpers;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

/// <summary>
/// The member-add strike rule (MEDIA-8): windows of n, 2n, 4n from each miss; the third miss
/// inside a window locks; a miss after the window starts over; a lock is never cleared by a miss.
/// The base window is one day here, so the doubling reads as 1, 2 and 4 days.
/// </summary>
[TestFixture]
public class MemberAddStrikePolicyTests
{
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
    private static readonly DateTimeOffset Start = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private IFixture _fixture = null!;
    private int _personId;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _personId = _fixture.Create<int>();
    }

    private PersonMemberAddStrikes Miss(PersonMemberAddStrikes? current, DateTimeOffset at) =>
        MemberAddStrikePolicy.AfterMiss(current, _personId, at, OneDay);

    [Test]
    public void AfterMiss_Should_StartAtOneWithTheBaseWindow_When_ThereAreNoStrikes()
    {
        var result = Miss(null, Start);

        result.PersonId.ShouldBe(_personId);
        result.StrikeCount.ShouldBe(1);
        result.WindowEndsOn.ShouldBe(Start.AddDays(1));
        result.LockedOn.ShouldBeNull();
        result.IsLocked.ShouldBeFalse();
        result.InsertedOn.ShouldBe(Start);
    }

    [Test]
    public void AfterMiss_Should_DoubleTheWindowFromNow_When_InsideTheWindow()
    {
        var first = Miss(null, Start);
        var secondAt = Start.AddHours(6);

        var second = Miss(first, secondAt);

        second.StrikeCount.ShouldBe(2);
        second.WindowEndsOn.ShouldBe(secondAt.AddDays(2));
        second.LockedOn.ShouldBeNull();
        second.InsertedOn.ShouldBe(first.InsertedOn);
    }

    [Test]
    public void AfterMiss_Should_UseWindowsOfOneTwoAndFourDays_And_LockOnTheThird()
    {
        var first = Miss(null, Start);
        var second = Miss(first, Start.AddHours(12));
        var thirdAt = Start.AddDays(2);
        var third = Miss(second, thirdAt);

        (first.WindowEndsOn - Start).ShouldBe(TimeSpan.FromDays(1));
        (second.WindowEndsOn - Start.AddHours(12)).ShouldBe(TimeSpan.FromDays(2));
        (third.WindowEndsOn - thirdAt).ShouldBe(TimeSpan.FromDays(4));
        first.IsLocked.ShouldBeFalse();
        second.IsLocked.ShouldBeFalse();
        third.StrikeCount.ShouldBe(MemberAddStrikePolicy.LockingStrike);
        third.LockedOn.ShouldBe(thirdAt);
    }

    [Test]
    public void AfterMiss_Should_StartOver_When_TheWindowHasEnded()
    {
        var first = Miss(null, Start);
        var second = Miss(first, Start.AddHours(1));
        var afterWindow = second.WindowEndsOn!.Value.AddMinutes(1);

        var result = Miss(second, afterWindow);

        result.StrikeCount.ShouldBe(1);
        result.WindowEndsOn.ShouldBe(afterWindow.AddDays(1));
        result.LockedOn.ShouldBeNull();
    }

    [Test]
    public void AfterMiss_Should_StartOver_When_TheMissLandsExactlyAsTheWindowEnds()
    {
        var first = Miss(null, Start);

        var result = Miss(first, first.WindowEndsOn!.Value);

        result.StrikeCount.ShouldBe(1);
    }

    [Test]
    public void AfterMiss_Should_StartOver_When_TheCountWasReset()
    {
        var reset = new PersonMemberAddStrikes { PersonId = _personId, StrikeCount = 0, WindowEndsOn = null, InsertedOn = Start };

        var result = Miss(reset, Start.AddDays(10));

        result.StrikeCount.ShouldBe(1);
        result.InsertedOn.ShouldBe(Start);
    }

    [Test]
    public void AfterMiss_Should_NotLock_When_ThreeMissesAreSpreadAcrossWindows()
    {
        var first = Miss(null, Start);
        var second = Miss(first, Start.AddHours(1));
        var restarted = Miss(second, second.WindowEndsOn!.Value.AddSeconds(1));

        restarted.StrikeCount.ShouldBe(1);
        restarted.IsLocked.ShouldBeFalse();
    }

    [Test]
    public void AfterMiss_Should_KeepTheFirstLockMoment_When_MissingAgainWhileLocked()
    {
        var lockedAt = Start.AddDays(1);
        var locked = new PersonMemberAddStrikes
        {
            PersonId = _personId,
            StrikeCount = 3,
            WindowEndsOn = lockedAt.AddDays(4),
            LockedOn = lockedAt,
            InsertedOn = Start
        };
        var fourthAt = lockedAt.AddHours(1);

        var result = Miss(locked, fourthAt);

        result.StrikeCount.ShouldBe(4);
        result.WindowEndsOn.ShouldBe(fourthAt.AddDays(8));
        result.LockedOn.ShouldBe(lockedAt);
    }

    [Test]
    public void AfterMiss_Should_KeepTheLock_When_TheWindowHasEnded()
    {
        var lockedAt = Start;
        var locked = new PersonMemberAddStrikes { PersonId = _personId, StrikeCount = 0, WindowEndsOn = null, LockedOn = lockedAt, InsertedOn = Start };

        var result = Miss(locked, Start.AddDays(30));

        result.StrikeCount.ShouldBe(1);
        result.LockedOn.ShouldBe(lockedAt);
    }

    [Test]
    public void AfterMiss_Should_StopDoubling_When_PastTheMaximum()
    {
        var current = new PersonMemberAddStrikes
        {
            PersonId = _personId,
            StrikeCount = 40,
            WindowEndsOn = Start.AddDays(1),
            LockedOn = Start,
            InsertedOn = Start
        };

        var result = Miss(current, Start);

        result.StrikeCount.ShouldBe(41);
        result.WindowEndsOn.ShouldBe(Start.AddDays(1 << MemberAddStrikePolicy.MaxDoublings));
    }

    [Test]
    public void AfterMiss_Should_ScaleWithTheConfiguredBaseWindow()
    {
        var baseWindow = TimeSpan.FromDays(3);
        var first = MemberAddStrikePolicy.AfterMiss(null, _personId, Start, baseWindow);
        var second = MemberAddStrikePolicy.AfterMiss(first, _personId, Start, baseWindow);
        var third = MemberAddStrikePolicy.AfterMiss(second, _personId, Start, baseWindow);

        first.WindowEndsOn.ShouldBe(Start.AddDays(3));
        second.WindowEndsOn.ShouldBe(Start.AddDays(6));
        third.WindowEndsOn.ShouldBe(Start.AddDays(12));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void AfterMiss_Should_Throw_When_TheBaseWindowIsNotPositive(int days)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MemberAddStrikePolicy.AfterMiss(null, _personId, Start, TimeSpan.FromDays(days)));
    }
}
