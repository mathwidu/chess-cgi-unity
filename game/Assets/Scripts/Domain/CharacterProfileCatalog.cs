using System.Collections.Generic;

public static class CharacterProfileCatalog
{
    private static readonly Dictionary<ChessPieceKind, CharacterProfile> Profiles = new Dictionary<ChessPieceKind, CharacterProfile>
    {
        {
            ChessPieceKind.Pawn,
            new CharacterProfile(
                ChessPieceKind.Pawn,
                "Peao",
                "Mathwidu",
                "Mathwidu",
                "Aluno",
                "Matricula nao informada",
                "Peao personalizado do projeto, com cabelo ruivo e visual casual para representar o autor no tabuleiro.",
                "Grounded walk",
                "Adaga curta em captura futura.")
        },
        {
            ChessPieceKind.Rook,
            new CharacterProfile(
                ChessPieceKind.Rook,
                "Torre",
                "Alex",
                "Alex",
                "Aluno",
                "Matricula nao informada",
                "Torre personalizada com Alex sentado em uma pequena torre, mantendo a silhueta forte da peca.",
                "Heavy hop",
                "Torre cai com impacto pesado em captura futura.")
        },
        {
            ChessPieceKind.Knight,
            new CharacterProfile(
                ChessPieceKind.Knight,
                "Cavalo",
                "Gustavo",
                "Gustavo",
                "Aluno",
                "Matricula nao informada",
                "Cavalo personalizado com Gustavo montado em um cavalo pequeno, criando uma leitura divertida da peca.",
                "Arcing L jump",
                "Relincho e salto em captura futura.")
        },
        {
            ChessPieceKind.Bishop,
            new CharacterProfile(
                ChessPieceKind.Bishop,
                "Bispo",
                "Rafael",
                "Rafael",
                "Aluno",
                "Matricula nao informada",
                "Bispo personalizado baseado no Rafael, mantendo postura vertical e identidade visual propria.",
                "Ritual stride",
                "Oracao/laser diagonal em captura futura.")
        },
        {
            ChessPieceKind.Queen,
            new CharacterProfile(
                ChessPieceKind.Queen,
                "Rainha",
                "Marta",
                "Professora Marta",
                "Professor",
                "Professor",
                "Rainha personalizada da professora Marta, com cachecol azul e branco como detalhe de destaque.",
                "Confident walk",
                "Golpe elegante com espada/energia em captura futura.")
        },
        {
            ChessPieceKind.King,
            new CharacterProfile(
                ChessPieceKind.King,
                "Rei",
                "Ricardo Carioca",
                "Professor Ricardo Carioca",
                "Professor",
                "Professor",
                "Rei personalizado do professor Ricardo Carioca, usando blusao azul da Feevale como referencia visual.",
                "Authoritative steps",
                "Golpe de mao aberta em captura futura.")
        }
    };

    public static CharacterProfile GetProfile(ChessPieceKind kind)
    {
        if (Profiles.TryGetValue(kind, out CharacterProfile profile))
        {
            return profile;
        }

        return new CharacterProfile(
            kind,
            "Peca",
            "Peca classica",
            "Peca classica",
            "Modelo classico",
            "Nao se aplica",
            "Modelo classico usado quando nao ha personagem personalizado.",
            "Classic move",
            "Captura classica.");
    }
}
