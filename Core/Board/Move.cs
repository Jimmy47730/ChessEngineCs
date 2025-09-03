using Core.Helpers;

namespace Core.Board
{
    public readonly struct Move
    {
        /*
            Moves are represented as a 23-bit unsigned integer (uint).
            KK MMM PPP C SSSS FFFFFF TTTTTT
            KK: Check flag
            MMM: Moved piece
            PPP: Promoted piece (0-4, where 0 is no promotion)
            C: Capture flag
            SSSS: Special move flags (e.g., castling, en passant, etc.)
            FFFFFF: From square (0-63)
            TTTTTT: To square (0-63)

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
        private const uint CheckMask = 0xC0000000u;
        private const uint MovedPieceMask = 0x3C000000u;
        private const uint PromotedPieceMask = 0x03000000u;
        private const uint CaptureMask = 0x00800000u;
        private const uint SpecialMoveMask = 0x007F0000u;
        private const uint FromSquareMask = 0x0000FF00u;
        private const uint ToSquareMask = 0x000000FFu;
        private const int CheckShift = 30;
        private const int MovedPieceShift = 26;
        private const int PromotedPieceShift = 24;
        private const int SpecialMoveShift = 16;
        private const int FromSquareShift = 8;
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