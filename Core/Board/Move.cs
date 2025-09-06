using Core.Helpers;

namespace Core.Board
{
    public readonly struct Move
    {
        /*
            Moves are represented as a 32-bit unsigned integer (uint).
            CCC MMMMM PPP SSSSSSS FFFFFF TTTTTT
            CCC: Check flag (bits 29-31, 3 bits)
            MMMMM: Moved piece (bits 24-28, 5 bits)
            PPP: Promoted piece (bits 21-23, 3 bits, 0-4, where 0 is no promotion)
            C: Capture flag (bit 20, 1 bit)
            SSSSSSS: Special move flags (bits 13-19, 7 bits, e.g., castling, en passant, etc.)
            FFFFFF: From square (bits 7-12, 6 bits, 0-63)
            TTTTTT: To square (bits 0-6, 6 bits, 0-63)

            When comparing two moves with == operator, only the from and to squares are compared.
            The special move type and capture flag are not considered in the comparison.
            This allows for easy comparison of moves without needing to check the special conditions.

            The string representation of the move follows the standard format and includes the from square (eg. e2-e4, Nf3-g5+, O-O, etc.)

            The default empty move is represented as 0, which is not a valid move.
        */

        // Constructors
        public Move(int from, int to, bool isCapture, PieceType movedPiece, PromotionType promotedPiece = PromotionType.None, MoveType specialMove = MoveType.Normal, CheckType checkType = CheckType.None)
        {
            if (from < 0 || from > 63) throw new ArgumentOutOfRangeException(nameof(from), "from must be between 0 and 63");
            if (to < 0 || to > 63) throw new ArgumentOutOfRangeException(nameof(to), "to must be between 0 and 63");

            uint md = 0u;

            md |= ((uint)checkType << CheckShift) & CheckMask;

            md |= ((uint)movedPiece << MovedPieceShift) & MovedPieceMask;

            md |= ((uint)promotedPiece << PromotedPieceShift) & PromotedPieceMask;

            if (isCapture) md |= CaptureMask;

            md |= ((uint)specialMove << SpecialMoveShift) & SpecialMoveMask;

            md |= ((uint)from << FromSquareShift) & FromSquareMask;
            md |= ((uint)to << ToSquareShift) & ToSquareMask;

            moveData = md;
        }

        // Move data
        private readonly uint moveData;

    // Masks and shifts for extracting move components
    // Expanded PromotedPiece to 3 bits (0-4), shifted other fields accordingly
    private const uint CheckMask = 0xE0000000u;         // 3 bits (bits 29-31)
    private const uint MovedPieceMask = 0x1F000000u;    // 5 bits (bits 24-28)
    private const uint PromotedPieceMask = 0x00E00000u; // 3 bits (bits 21-23)
    private const uint CaptureMask = 0x00100000u;       // 1 bit  (bit 20)
    private const uint SpecialMoveMask = 0x000FE000u;   // 7 bits (bits 13-19)
    private const uint FromSquareMask = 0x00001F80u;    // 6 bits (bits 7-12)
    private const uint ToSquareMask = 0x0000007Fu;      // 6 bits (bits 0-6)
    private const int CheckShift = 29;
    private const int MovedPieceShift = 24;
    private const int PromotedPieceShift = 21;
    private const int SpecialMoveShift = 13;
    private const int FromSquareShift = 7;
    private const int ToSquareShift = 0;

        // Properties to access move components
        public bool IsValid => moveData != 0u;
        public bool IsCheck => ((moveData & CheckMask) >> CheckShift) == 1u;
        public bool IsMate => ((moveData & CheckMask) >> CheckShift) == 2u;
        public bool IsAnyCheck => (moveData & CheckMask) != 0u;
        public bool IsCapture => (moveData & CaptureMask) != 0u;
        public bool IsEnPassant => SpecialMove == MoveType.EnPassant;
        public bool IsPromotion => PromotedPiece != PromotionType.None;
        public bool IsCastling => SpecialMove == MoveType.ShortCastle || SpecialMove == MoveType.LongCastle;
        public int FromSquare => (int)((moveData & FromSquareMask) >> FromSquareShift);
        public int ToSquare => (int)((moveData & ToSquareMask) >> ToSquareShift);
        public PieceType MovedPiece => (PieceType)((moveData & MovedPieceMask) >> MovedPieceShift);
        public PromotionType PromotedPiece => (PromotionType)((moveData & PromotedPieceMask) >> PromotedPieceShift);
        public MoveType SpecialMove => (MoveType)((moveData & SpecialMoveMask) >> SpecialMoveShift);
        public CheckType CheckType => (CheckType)((moveData & CheckMask) >> CheckShift);

        // To string override
        public override string ToString()
        {
            if (!IsValid) return "N/A";

            string movedPiece = StringRepr.PieceConversion(MovedPiece);
            string from = Square.Name(FromSquare);
            string capture = IsCapture ? "x" : "-";
            string to = Square.Name(ToSquare);
            string promotion = StringRepr.PromotionConversion(PromotedPiece);

            return $"{movedPiece}{from}{capture}{to}{promotion}";
        }

        public string ToUci()
        {
            if (!IsValid) return "0000";

            return StringRepr.MoveUciConversion(this);
        }

        // Equals operator override to compare just from and to squares
        public override bool Equals(object? obj)
        {
            if (obj is Move other)
            {
                return FromSquare == other.FromSquare && ToSquare == other.ToSquare;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FromSquare, ToSquare);
        }

        public static bool operator ==(Move left, Move right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Move left, Move right)
        {
            return !left.Equals(right);
        }

        public static implicit operator bool(Move move)
        {
            return move.IsValid;
        }
    }
}