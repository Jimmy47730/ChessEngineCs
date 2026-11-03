using Core.DataStructures;

namespace Core.Helpers
{
    public static class ZobristHasher
    {
        private const int BoardSquares = 64;
        private const int PieceTypes = 12; // 6 per color
        private static readonly ulong[,] PieceSquareRandoms;
        private static readonly ulong[] CastlingRandoms;
        private static readonly ulong[] EnPassantRandoms;
        private static readonly ulong SideToMoveRandom;
        private static readonly int CastlingRightsCount = 4; // KQkq
        private static readonly int EnPassantFiles = 8;

        static ZobristHasher()
        {
            var rng = new Random(20240526); // Fixed seed for reproducibility
            PieceSquareRandoms = new ulong[PieceTypes, BoardSquares];
            for (int p = 0; p < PieceTypes; p++)
                for (int sq = 0; sq < BoardSquares; sq++)
                    PieceSquareRandoms[p, sq] = NextUlong(rng);

            CastlingRandoms = new ulong[CastlingRightsCount];
            for (int i = 0; i < CastlingRightsCount; i++)
                CastlingRandoms[i] = NextUlong(rng);

            EnPassantRandoms = new ulong[EnPassantFiles];
            for (int i = 0; i < EnPassantFiles; i++)
                EnPassantRandoms[i] = NextUlong(rng);

            SideToMoveRandom = NextUlong(rng);
        }

        private static ulong NextUlong(Random rng)
        {
            var buffer = new byte[8];
            rng.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        // Computes the zobrist hash from the board state parsed by the Board constructor.
        // squares are ordered from a1 to h8 and contain the FEN character for each piece,
        // or a space for an empty square.
        public static ulong ComputeHash(char[] squares, SideColor sideToMove, int enPassantSquare, bool[] castlingRights)
        {
            ulong hash = 0UL;
            if (squares == null || squares.Length != BoardSquares)
                throw new ArgumentException("The board must contain exactly 64 squares.", nameof(squares));

            for (int square = 0; square < BoardSquares; square++)
            {
                int pieceIndex = PieceCharToIndex(squares[square]);
                if (pieceIndex >= 0)
                    hash ^= PieceSquareRandoms[pieceIndex, square];
            }

            // Castling rights (KQkq).
            if (castlingRights[0]) hash ^= CastlingRandoms[0];
            if (castlingRights[1]) hash ^= CastlingRandoms[1];
            if (castlingRights[2]) hash ^= CastlingRandoms[2];
            if (castlingRights[3]) hash ^= CastlingRandoms[3];

            // En passant squares share a hash key by file.
            if (enPassantSquare >= 0 && enPassantSquare < BoardSquares)
                hash ^= EnPassantRandoms[enPassantSquare % EnPassantFiles];

            // Side to move
            if (sideToMove == SideColor.Black)
                hash ^= SideToMoveRandom;
            return hash;
        }

        // Helper: map piece char to index (P=0, N=1, ..., k=11)
        private static int PieceCharToIndex(char piece)
        {
            return piece switch
            {
                'P' => 0,
                'N' => 1,
                'B' => 2,
                'R' => 3,
                'Q' => 4,
                'K' => 5,
                'p' => 6,
                'n' => 7,
                'b' => 8,
                'r' => 9,
                'q' => 10,
                'k' => 11,
                _ => -1
            };
        }
    }
}
