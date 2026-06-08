using System.IO;
using NUnit.Framework;
using UnityEngine;

public class CharacterRigAuditTests
{
    private static readonly string[] RequiredCharacters =
    {
        "Pawn_Mathwidu_Redhead_v2",
        "Rook_Alex",
        "Knight_Gustavo",
        "Bishop_Rafael",
        "Queen_Marta",
        "King_Ricardo_Carioca"
    };

    [Test]
    public void AuditDocument_ExistsAndCoversEveryCustomCharacter()
    {
        string audit = LoadAuditDocument();

        foreach (string character in RequiredCharacters)
        {
            StringAssert.Contains($"## {character}", audit, character);
        }
    }

    [Test]
    public void AuditDocument_ClassifiesEveryCharacterBeforeRigging()
    {
        string audit = LoadAuditDocument();

        foreach (string character in RequiredCharacters)
        {
            int sectionStart = audit.IndexOf($"## {character}", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(sectionStart, 0, character);
            int nextSection = audit.IndexOf("\n## ", sectionStart + 1, System.StringComparison.Ordinal);
            string section = nextSection >= 0 ? audit.Substring(sectionStart, nextSection - sectionStart) : audit.Substring(sectionStart);

            StringAssert.Contains("Classificacao inicial:", section, character);
            StringAssert.Contains("Decisao antes de gastar creditos:", section, character);
            StringAssert.Contains("Validacao pendente:", section, character);
        }
    }

    private static string LoadAuditDocument()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        string auditPath = Path.Combine(projectRoot, "docs", "design", "character-rig-audit.md");
        Assert.IsTrue(File.Exists(auditPath), auditPath);
        return File.ReadAllText(auditPath);
    }
}
