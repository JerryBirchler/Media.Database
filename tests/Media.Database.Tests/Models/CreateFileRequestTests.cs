using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class CreateFileRequestTests
{
    [Test, AutoData]
    public void This_Should_Allow_PropertyAssignment(CreateFileRequest req)
    {
        // AutoFixture will populate the request; just assert properties roundtrip
        req.ShouldNotBeNull();
        req.SourceMachineId.ShouldBeGreaterThanOrEqualTo(0);
        req.OriginalFilePath.ShouldNotBeNullOrEmpty();
    }
}
