public sealed class CharacterProfile
{
    public CharacterProfile(
        ChessPieceKind kind,
        string pieceName,
        string displayName,
        string fullName,
        string category,
        string registration,
        string description)
    {
        Kind = kind;
        PieceName = pieceName;
        DisplayName = displayName;
        FullName = fullName;
        Category = category;
        Registration = registration;
        Description = description;
    }

    public ChessPieceKind Kind { get; }

    public string PieceName { get; }

    public string DisplayName { get; }

    public string FullName { get; }

    public string Category { get; }

    public string Registration { get; }

    public string Description { get; }
}
