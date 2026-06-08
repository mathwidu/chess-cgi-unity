using NUnit.Framework;

public class CharacterCandidateManifestTests
{
    [Test]
    public void CanImportIntoResources_ReturnsFalseWhenCandidateIsNotApproved()
    {
        CharacterCandidateManifest manifest = new CharacterCandidateManifest
        {
            ApprovedForUnity = false,
            ReplacesActivePrefab = false
        };

        Assert.IsFalse(manifest.CanImportIntoResources());
    }

    [Test]
    public void CanImportIntoResources_ReturnsTrueOnlyWhenApprovedAndReplacingIsExplicit()
    {
        CharacterCandidateManifest manifest = new CharacterCandidateManifest
        {
            ApprovedForUnity = true,
            ReplacesActivePrefab = true
        };

        Assert.IsTrue(manifest.CanImportIntoResources());
    }

    [Test]
    public void FromJson_MapsCamelCaseManifestFields()
    {
        CharacterCandidateManifest manifest = CharacterCandidateManifest.FromJson(
            "{\"approvedForUnity\":true,\"replacesActivePrefab\":false,\"candidateModelPath\":\"Assets/Art/Candidate.glb\"}");

        Assert.IsTrue(manifest.ApprovedForUnity);
        Assert.IsFalse(manifest.ReplacesActivePrefab);
        Assert.AreEqual("Assets/Art/Candidate.glb", manifest.CandidateModelPath);
        Assert.IsFalse(manifest.CanImportIntoResources());
    }
}
