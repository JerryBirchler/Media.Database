using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class UpdateFileRequestTests
{
    [Test, AutoData]
    public void UpdateFileRequest_Should_Allow_PropertyAssignment(UpdateFileRequest r)
    {
        r.ShouldNotBeNull();
        // Properties may be null depending on AutoFixture customization; ensure access does not throw
        _ = r.LastFileUpdate;
        _ = r.Metadata;
    }
}
