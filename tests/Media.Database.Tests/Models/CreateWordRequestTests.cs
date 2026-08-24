using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class CreateWordRequestTests
{
    [Test, AutoData]
    public void CreateWordRequest_Should_Allow_PropertyAssignment(CreateWordRequest req)
    {
        req.ShouldNotBeNull();
        req.Word.ShouldNotBeNullOrEmpty();
        req.CameFromFileId.ShouldNotBe(Guid.Empty);
    }
}
