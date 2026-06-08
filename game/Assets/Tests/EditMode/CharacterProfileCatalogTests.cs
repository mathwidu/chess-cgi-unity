using NUnit.Framework;

public class CharacterProfileCatalogTests
{
    [Test]
    public void GetProfile_ReturnsKnownCharacterForEachChessKind()
    {
        Assert.AreEqual("Mathwidu", CharacterProfileCatalog.GetProfile(ChessPieceKind.Pawn).DisplayName);
        Assert.AreEqual("Alex", CharacterProfileCatalog.GetProfile(ChessPieceKind.Rook).DisplayName);
        Assert.AreEqual("Gustavo", CharacterProfileCatalog.GetProfile(ChessPieceKind.Knight).DisplayName);
        Assert.AreEqual("Rafael", CharacterProfileCatalog.GetProfile(ChessPieceKind.Bishop).DisplayName);
        Assert.AreEqual("Marta", CharacterProfileCatalog.GetProfile(ChessPieceKind.Queen).DisplayName);
        Assert.AreEqual("Ricardo Carioca", CharacterProfileCatalog.GetProfile(ChessPieceKind.King).DisplayName);
    }

    [Test]
    public void GetProfile_UsesSafePrivateDataDefaults()
    {
        CharacterProfile pawn = CharacterProfileCatalog.GetProfile(ChessPieceKind.Pawn);
        CharacterProfile queen = CharacterProfileCatalog.GetProfile(ChessPieceKind.Queen);

        Assert.AreEqual("Aluno", pawn.Category);
        Assert.AreEqual("Professor", queen.Category);
        Assert.AreEqual("Matricula nao informada", pawn.Registration);
        Assert.AreEqual("Professor", queen.Registration);
    }

    [Test]
    public void GetProfile_ContainsSidebarReadyText()
    {
        CharacterProfile rook = CharacterProfileCatalog.GetProfile(ChessPieceKind.Rook);

        Assert.AreEqual("Torre", rook.PieceName);
        StringAssert.Contains("torre", rook.Description.ToLowerInvariant());
        Assert.IsFalse(string.IsNullOrWhiteSpace(rook.FullName));
    }

    [Test]
    public void GetProfile_ContainsMovementAndCapturePlanningText()
    {
        CharacterProfile pawn = CharacterProfileCatalog.GetProfile(ChessPieceKind.Pawn);
        CharacterProfile knight = CharacterProfileCatalog.GetProfile(ChessPieceKind.Knight);

        Assert.AreEqual("Grounded walk", pawn.MovementStyle);
        StringAssert.Contains("Relincho", knight.CaptureConcept);
    }
}
