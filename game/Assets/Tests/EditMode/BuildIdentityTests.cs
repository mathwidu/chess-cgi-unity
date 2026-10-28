using NUnit.Framework;

public class BuildIdentityTests
{
    [Test]
    public void BuildTimeUsesBrasiliaWithDateRolloverAndRemainsStable()
    {
        var identity = AndroidIdentity();
        string expected = "v0.1.1 (2) · 26/10/2026 22:02:03 Brasília (UTC-3)";
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(identity, "0.1.1", true), Is.EqualTo(expected));
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(identity, "0.1.1", true), Is.EqualTo(expected));
    }

    [Test]
    public void DifferentApplicationVersionDoesNotShowAnOldTimestamp()
    {
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(AndroidIdentity(), "0.1.2", true),
            Is.EqualTo("v0.1.2 · data da compilação indisponível"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("invalid timestamp")]
    public void MissingOrInvalidTimestampIsExplicit(string timestamp)
    {
        var identity = AndroidIdentity();
        identity.builtAtUtc = timestamp;
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(identity, "0.1.1", true),
            Is.EqualTo("v0.1.1 · data da compilação indisponível"));
    }

    [Test]
    public void MissingIdentityOrAnotherPlatformCannotIdentifyAnAndroidBuild()
    {
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(null, "0.1.1", true), Does.Contain("indisponível"));
        var identity = AndroidIdentity();
        identity.buildTarget = "StandaloneOSX";
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(identity, "0.1.1", true), Does.Contain("indisponível"));
        identity.buildTarget = "Android";
        identity.androidVersionCode = 0;
        Assert.That(ChessCgiBuildIdentity.FormatMenuLabel(identity, "0.1.1", true), Does.Contain("indisponível"));
    }

    private static ChessCgiBuildIdentity AndroidIdentity() => new ChessCgiBuildIdentity
    {
        versionName = "0.1.1", androidVersionCode = 2, buildTarget = "Android",
        builtAtUtc = "2026-10-27T01:02:03.0000000+00:00"
    };
}
