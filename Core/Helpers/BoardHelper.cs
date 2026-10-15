using Core.Helpers;
using Core.Board;
namespace Core.Helpers;

public static class BoardHelper
{
    public static BitBoard[] CreateBitboards(char[] pieceArray)
    {
        var bitboards = new BitBoard[12];
        for (int i = 0; i < pieceArray.Length; i++)
        {
            char piece = pieceArray[i];
            if (piece != '\0')
            {
                int pieceIndex = BitBoard.PieceIndex(piece);
                bitboards[pieceIndex] |= 1UL << i;
            }
        }
        return bitboards;
    }

    public static PieceName GetPieceName(char pieceChar)
    {
        return pieceChar switch
        {
            'P' => PieceName.WhitePawn,
            'N' => PieceName.WhiteKnight,
            'B' => PieceName.WhiteBishop,
            'R' => PieceName.WhiteRook,
            'Q' => PieceName.WhiteQueen,
            'K' => PieceName.WhiteKing,
            'p' => PieceName.BlackPawn,
            'n' => PieceName.BlackKnight,
            'b' => PieceName.BlackBishop,
            'r' => PieceName.BlackRook,
            'q' => PieceName.BlackQueen,
            'k' => PieceName.BlackKing,
            _ => PieceName.None,
        };
    }

    public static char GetPieceChar(PieceName pieceName)
    {
        return pieceName switch
        {
            PieceName.WhitePawn => 'P',
            PieceName.WhiteKnight => 'N',
            PieceName.WhiteBishop => 'B',
            PieceName.WhiteRook => 'R',
            PieceName.WhiteQueen => 'Q',
            PieceName.WhiteKing => 'K',
            PieceName.BlackPawn => 'p',
            PieceName.BlackKnight => 'n',
            PieceName.BlackBishop => 'b',
            PieceName.BlackRook => 'r',
            PieceName.BlackQueen => 'q',
            PieceName.BlackKing => 'k',
            _ => '\0',
        };
    }

    public static PieceType GetPieceType(PieceName pieceName)
    {
        return pieceName switch
        {
            PieceName.WhitePawn or PieceName.BlackPawn => PieceType.Pawn,
            PieceName.WhiteKnight or PieceName.BlackKnight => PieceType.Knight,
            PieceName.WhiteBishop or PieceName.BlackBishop => PieceType.Bishop,
            PieceName.WhiteRook or PieceName.BlackRook => PieceType.Rook,
            PieceName.WhiteQueen or PieceName.BlackQueen => PieceType.Queen,
            PieceName.WhiteKing or PieceName.BlackKing => PieceType.King,
            _ => throw new ArgumentOutOfRangeException(nameof(pieceName), $"Invalid piece name {pieceName}")
        };
    }

    public static PieceName GetPieceName(SideColor color, PieceType pieceType)
    {
        return (color, pieceType) switch
        {
            (SideColor.White, PieceType.Pawn) => PieceName.WhitePawn,
            (SideColor.White, PieceType.Knight) => PieceName.WhiteKnight,
            (SideColor.White, PieceType.Bishop) => PieceName.WhiteBishop,
            (SideColor.White, PieceType.Rook) => PieceName.WhiteRook,
            (SideColor.White, PieceType.Queen) => PieceName.WhiteQueen,
            (SideColor.White, PieceType.King) => PieceName.WhiteKing,
            (SideColor.Black, PieceType.Pawn) => PieceName.BlackPawn,
            (SideColor.Black, PieceType.Knight) => PieceName.BlackKnight,
            (SideColor.Black, PieceType.Bishop) => PieceName.BlackBishop,
            (SideColor.Black, PieceType.Rook) => PieceName.BlackRook,
            (SideColor.Black, PieceType.Queen) => PieceName.BlackQueen,
            (SideColor.Black, PieceType.King) => PieceName.BlackKing,
            _ => throw new ArgumentOutOfRangeException($"Invalid color/piece combination: {color}, {pieceType}")
        };
    }

    public static bool AreDiagonallyAligned(int square1, int square2)
    {
        int rank1 = square1 / 8, file1 = square1 % 8;
        int rank2 = square2 / 8, file2 = square2 % 8;

        return Math.Abs(rank1 - rank2) == Math.Abs(file1 - file2);
    }
    public static bool AreHorizontallyAligned(int square1, int square2)
    {
        int rank1 = square1 / 8;
        int rank2 = square2 / 8;

        return rank1 == rank2;
    }
    public static bool AreVerticallyAligned(int square1, int square2)
    {
        int file1 = square1 % 8;
        int file2 = square2 % 8;

        return file1 == file2;
    }

    public static ulong GetDiagonalLineBitboard(int square1, int square2, bool includeExtremes)
    {
        if (!AreDiagonallyAligned(square1, square2))
        {
            return 0;
        }

        int rank1 = square1 / 8;
        int file1 = square1 % 8;
        int rank2 = square2 / 8;
        int file2 = square2 % 8;

        if (square1 == square2)
        {
            return includeExtremes ? 1UL << square1 : 0;
        }

        int rankStep = Math.Sign(rank2 - rank1);
        int fileStep = Math.Sign(file2 - file1);
        ulong lineBitboard = 0;

        for (int rank = rank1, file = file1;
             rank != rank2;
             rank += rankStep, file += fileStep)
        {
            if (includeExtremes || (rank != rank1 && rank != rank2))
            {
                lineBitboard |= 1UL << (rank * 8 + file);
            }
        }

        return lineBitboard;
    }

    public static ulong GetHorizontalLineBitboard(int square1, int square2, bool includeExtremes)
    {
        if (!AreHorizontallyAligned(square1, square2))
        {
            return 0;
        }

        int startSquare = Math.Min(square1, square2);
        int endSquare = Math.Max(square1, square2);
        ulong lineBitboard = 0;

        for (int square = startSquare; square <= endSquare; square++)
        {
            if (includeExtremes || (square > startSquare && square < endSquare))
            {
                lineBitboard |= 1UL << square;
            }
        }

        return lineBitboard;
    }

    public static ulong GetVerticalLineBitboard(int square1, int square2, bool includeExtremes)
    {
        if (!AreVerticallyAligned(square1, square2))
        {
            return 0;
        }

        int startSquare = Math.Min(square1, square2);
        int endSquare = Math.Max(square1, square2);
        ulong lineBitboard = 0;

        for (int square = startSquare; square <= endSquare; square += 8)
        {
            if (includeExtremes || (square > startSquare && square < endSquare))
            {
                lineBitboard |= 1UL << square;
            }
        }

        return lineBitboard;
    }

    public static ulong GetLineBitboard(int square1, int square2, bool includeExtremes)
    {
        if (AreDiagonallyAligned(square1, square2))
        {
            return GetDiagonalLineBitboard(square1, square2, includeExtremes);
        }
        else if (AreHorizontallyAligned(square1, square2))
        {
            return GetHorizontalLineBitboard(square1, square2, includeExtremes);
        }
        else if (AreVerticallyAligned(square1, square2))
        {
            return GetVerticalLineBitboard(square1, square2, includeExtremes);
        }
        else
        {
            return 0;
        }
    }
}