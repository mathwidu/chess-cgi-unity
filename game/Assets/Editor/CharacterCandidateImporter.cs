using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CharacterCandidateImporter
{
    public static CharacterCandidateManifest LoadManifestAtPath(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("Manifest path cannot be empty.", nameof(manifestPath));
        }

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Character candidate manifest not found.", manifestPath);
        }

        return CharacterCandidateManifest.FromJson(File.ReadAllText(manifestPath));
    }

    public static GameObject ImportApprovedCandidate(CharacterCandidateManifest manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (!manifest.CanImportIntoResources())
        {
            throw new InvalidOperationException("Character candidate is not approved for Unity import.");
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(manifest.CandidateModelPath);
        if (model == null)
        {
            throw new InvalidOperationException($"Candidate model not found: {manifest.CandidateModelPath}");
        }

        return model;
    }
}
