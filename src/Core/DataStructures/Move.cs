using Core.Helpers;

namespace Core.DataStructures;

public readonly struct Move
{
    /*
        From-to based 16-bit move representation.
        The 16 bits are organized as follows:
        PCSS FFFFFF TTTTTT
            P: promotion
            C: capture
            S: special flags (work with the other two flags to specify every special move)
            F: from square (6 bits)
            T: to square (6 bits)
        An invalid/empty move is represented by a value of 0.
        The codes range 0-15 with the exclusion of 6-7 (yeah i know omg 67) following the table at the link below
        See further explanation at https://chessprogramming.org/Encoding_Moves#from-to-based
    */

    private readonly ushort move;

    public Move(int from, int to, int flags = 0)
    {
        move = (ushort)((to & 0x3F) | ((from & 0x3F) << 6) | ((flags & 0x0F) << 12));
    }

    // Methods to generate the special move flags based on the move type

    public static int GeneratePromotionFlag(PieceType pieceType, bool isCapture)
    {
        int promotionFlag = 0b1000; // The promotion bit is always set for a promotion
        int captureFlag = isCapture ? 0b0100 : 0;
        promotionFlag |= captureFlag; // Set the capture bit if it's a capture move
        
        pieceType -= 1; // Adjust PieceType to match the encoding (Knight=0, Bishop=1, Rook=2, Queen=3)
        if ((int)pieceType < 0 || (int)pieceType > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(pieceType), "Invalid promotion piece type.");
        }

        return promotionFlag | (int)pieceType;
    }
    public static int GenerateSpecialMoveFlag(CaptureType captureType, CastlingType castlingType, bool isDoublePawnPush)
    {
        int specialFlag;
        specialFlag = captureType switch
        {
            CaptureType.Normal => 0b0100,
            CaptureType.EnPassant => 0b0101,
            _ => 0
        };
        if (specialFlag != 0) return specialFlag;

        specialFlag = castlingType switch
        {
            CastlingType.KingSide => 0b0010,
            CastlingType.QueenSide => 0b0011,
            _ => 0,
        };
        if (specialFlag != 0) return specialFlag;

        if (isDoublePawnPush) return 0b0001;

        return 0; // No special move
    }

    // Overloads for special move flags
    public static int GenerateSpecialMoveFlag(CaptureType captureType) => GenerateSpecialMoveFlag(captureType, CastlingType.None, false);
    public static int GenerateSpecialMoveFlag(CastlingType castlingType) => GenerateSpecialMoveFlag(CaptureType.None, castlingType, false);
    public static int GenerateSpecialMoveFlag(bool isDoublePawnPush) => GenerateSpecialMoveFlag(CaptureType.None, CastlingType.None, isDoublePawnPush);


    // Methods to get information about the move
    public readonly int From() => (move >> 6) & 0x3F;
    public readonly int To() => move & 0x3F;
    public readonly int Flags() => (move >> 12) & 0x0F;
    public readonly bool IsPromotion() => (Flags() & 0b1000) == 1;
    public readonly bool IsCapture() => (Flags() & 0b0100) == 1;
    public readonly bool IsDoublePawnPush() => Flags() == 0b0001;
    public readonly bool IsEnPassant() => Flags() == 0b0101;
    public readonly bool IsKingSideCastle() => Flags() == 0b0010;
    public readonly bool IsQueenSideCastle() => Flags() == 0b0011;
    public readonly bool IsCastling() => IsKingSideCastle() || IsQueenSideCastle();
    public readonly PieceType PromotionPieceType()
    {
        if (!IsPromotion()) return PieceType.None;
        int promotionFlag = Flags() & 0b0011; // Get the last two bits for the promotion piece type
        return promotionFlag switch
        {
            0 => PieceType.Knight,
            1 => PieceType.Bishop,
            2 => PieceType.Rook,
            3 => PieceType.Queen,
            _ => throw new InvalidOperationException("Invalid promotion piece type.")
        };
    }

    public readonly bool IsValid() => move != 0;
}