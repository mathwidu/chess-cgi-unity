using System;

public readonly struct BoardSquare : IEquatable<BoardSquare>
{
    public int FileIndex { get; }
    public int Rank { get; }

    public BoardSquare(int fileIndex, int rank)
    {
        if (fileIndex < 0 || fileIndex > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(fileIndex), "File index must be between 0 and 7.");
        }

        if (rank < 1 || rank > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be between 1 and 8.");
        }

        FileIndex = fileIndex;
        Rank = rank;
    }

    public static BoardSquare FromAlgebraic(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 2)
        {
            throw new ArgumentException("Square must use algebraic notation like e4.", nameof(value));
        }

        char file = char.ToLowerInvariant(value[0]);
        char rankChar = value[1];

        if (file < 'a' || file > 'h' || rankChar < '1' || rankChar > '8')
        {
            throw new ArgumentException("Square must be between a1 and h8.", nameof(value));
        }

        return new BoardSquare(file - 'a', rankChar - '0');
    }

    public string ToAlgebraic()
    {
        char file = (char)('a' + FileIndex);
        return $"{file}{Rank}";
    }

    public bool Equals(BoardSquare other)
    {
        return FileIndex == other.FileIndex && Rank == other.Rank;
    }

    public override bool Equals(object obj)
    {
        return obj is BoardSquare other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileIndex, Rank);
    }

    public override string ToString()
    {
        return ToAlgebraic();
    }
}
