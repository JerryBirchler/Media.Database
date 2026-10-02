using AutoFixture;
using Media.Common.Archetypes;
using System.Threading;

namespace Media.Database.Tests.TestHelpers;

/// <summary>
/// Test-only shorthands for the archetypes (COMMON-17): "jerry@example.com".Email() reads better in
/// a test than EmailAddress.Parse(...). Deliberately absent from production code, where a raw string
/// must never become an archetype without being parsed on purpose.
/// </summary>
internal static class ArchetypeTestExtensions
{
    public static EmailAddress Email(this string value) => EmailAddress.Parse(value);

    public static PhoneNumber Phone(this string value) => PhoneNumber.Parse(value);

    public static PersonName Name(this string value) => PersonName.Parse(value);
}

/// <summary>
/// Teaches AutoFixture to build valid, distinct archetypes. Without it, fixture.Create of a type
/// holding one yields the never-parsed default -- an empty value no real request can carry.
/// </summary>
internal sealed class ArchetypeCustomization : ICustomization
{
    private static int _next;

    public void Customize(IFixture fixture)
    {
        fixture.Register(() => EmailAddress.Parse($"person{Interlocked.Increment(ref _next)}@example.test"));
        fixture.Register(() => PhoneNumber.Parse($"+1214555{Interlocked.Increment(ref _next) % 10000:0000}"));
        fixture.Register(() => PersonName.Parse($"Name{Interlocked.Increment(ref _next)}"));
    }
}
