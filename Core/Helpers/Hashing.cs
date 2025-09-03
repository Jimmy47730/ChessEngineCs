using System;
using Core.Helpers;
using Core.Board;

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

        // Computes the zobrist hash for a given board state
        public static ulong ComputeHash(BoardState state)
        {
            ulong hash = 0UL;
            var pieceArray = state.PieceArray;
            for (int sq = 0; sq < 64; sq++)
            {
                char piece = pieceArray[sq];
                if (piece == '\0') continue;
                int pieceIndex = PieceCharToIndex(piece);
                if (pieceIndex >= 0)
                    hash ^= PieceSquareRandoms[pieceIndex, sq];
            }
            // Castling rights (assume order: WhiteK, WhiteQ, BlackK, BlackQ)
            var castling = state.CastlingRights;
            for (int i = 0; i < CastlingRightsCount; i++)
                if (castling[i]) hash ^= CastlingRandoms[i];
            // En passant
            int ep = state.EnPassantSquare;
            if (ep >= 0 && ep < 64)
            {
                int file = ep % 8;
                hash ^= EnPassantRandoms[file];
            }
            // Side to move
            if (state.SideToMove == SideColor.Black)
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
