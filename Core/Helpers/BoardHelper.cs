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
}