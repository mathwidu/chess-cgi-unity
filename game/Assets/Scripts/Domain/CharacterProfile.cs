public sealed class CharacterProfile
{
    public CharacterProfile(
        ChessPieceKind kind,
        string pieceName,
        string displayName,
        string fullName,
        string category,
        string registration,
        string description,
        string movementStyle,
        string captureConcept)
    {
        Kind = kind;
        PieceName = pieceName;
        DisplayName = displayName;
        FullName = fullName;
        Category = category;
        Registration = registration;
        Description = description;
        MovementStyle = movementStyle;
        CaptureConcept = captureConcept;
    }

    public ChessPieceKind Kind { get; }

    public string PieceName { get; }

    public string DisplayName { get; }

    public string FullName { get; }

    public string Category { get; }

    public string Registration { get; }

    public string Description { get; }

    public string MovementStyle { get; }

    public string CaptureConcept { get; }
}
