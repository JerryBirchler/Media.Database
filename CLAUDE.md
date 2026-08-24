# Media.Database

## Unit testing conventions

- **Frameworks**: NUnit + Moq + Shouldly + AutoFixture. Use all four together.
- **Naming**: `MethodUnderTest_Should_ExpectedResultDescription` — the first segment must name the actual method under test, not a vague placeholder like `This_Should_...`. For plain POCO/record/DTO tests with no real method, `ClassName_Should_...` is the accepted substitution.
- **Test data**: prefer an `AutoMoqFixture` helper (`new Fixture().Customize(new AutoMoqCustomization())`) — `fixture.Freeze<Mock<IDependency>>()` for mocks you configure/verify, `fixture.Create<Sut>()` to auto-wire the SUT from frozen mocks, `fixture.Create<T>()`/`CreateMany<T>()` for incidental data. Keep literals only where the literal value is the actual point of the test (null/whitespace edge cases, casing-sensitive behavior, specific invalid-syntax strings).

Full conventions, including known gotchas (AutoFixture crashing on ASP.NET-style base classes, internal-setter properties needing a reflection helper, testing fire-and-forget `Task.Run` background work, Moq callback parameter counts) live in the global `dotnet-test-standards` Claude Code skill — apply the same standard here.

## Code coverage

Run `./scripts/coverage-report.ps1` from the repo root to generate an HTML coverage report at `CoverageReport/index.html` (gitignored). This repo has a single test project (`tests/Media.Database.Tests`), so it's auto-discovered — no `-TestProject` needed.

This never touches Visual Studio's own coverage tooling, so it never leaves line-coloring in the editor.
