using System;
using UnityEngine;

[Serializable]
public sealed class CharacterCandidateManifest
{
    [SerializeField] private string candidateId;
    [SerializeField] private string personName;
    [SerializeField] private string pieceKind;
    [SerializeField] private string candidateModelPath;
    [SerializeField] private bool approvedForUnity;
    [SerializeField] private bool replacesActivePrefab;

    public string CandidateId
    {
        get => candidateId;
        set => candidateId = value;
    }

    public string PersonName
    {
        get => personName;
        set => personName = value;
    }

    public string PieceKind
    {
        get => pieceKind;
        set => pieceKind = value;
    }

    public string CandidateModelPath
    {
        get => candidateModelPath;
        set => candidateModelPath = value;
    }

    public bool ApprovedForUnity
    {
        get => approvedForUnity;
        set => approvedForUnity = value;
    }

    public bool ReplacesActivePrefab
    {
        get => replacesActivePrefab;
        set => replacesActivePrefab = value;
    }

    public static CharacterCandidateManifest FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Manifest JSON cannot be empty.", nameof(json));
        }

        return JsonUtility.FromJson<CharacterCandidateManifest>(json);
    }

    public bool CanImportIntoResources()
    {
        return approvedForUnity && replacesActivePrefab;
    }
}
