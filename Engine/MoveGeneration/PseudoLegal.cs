using Core.Helpers;
using Core.Board;

namespace Engine.MoveGeneration
{
    public class PseudoLegal
    {
        private static PseudoLegal instance = new();
        private static readonly object lockObj = new();
        private readonly Magics magics;
        private BoardState boardState;
        private SideColor CurrentColor => boardState.SideToMove;

        // Constructor and Singleton Instance
        public static PseudoLegal Instance()
        {
            if (instance == null)
            {
                lock (lockObj)
                {
                    instance ??= new PseudoLegal();
                }
            }
            return instance;
        }

        private PseudoLegal()
        {
            magics = Magics.Instance();
        }

        // Move generation methods
        // Rewrite this method to generate king moves first
        public List<Move> GenerateMoves(BoardState boardState)
        {
            this.boardState = boardState;
            List<Move> moves = [];

            PieceName kingPiece = CurrentColor == SideColor.White ? PieceName.WhiteKing : PieceName.BlackKing;
            BitBoard kingBitboard = boardState.GetBitboard(kingPiece);
            moves.AddRange(GenerateMovesForPiece(kingPiece, kingBitboard));

            int startIndex = CurrentColor == SideColor.White ? 0 : 6;
            int endIndex = startIndex + 5; // Exclude the king (already added)

            for (int i = startIndex; i < endIndex; i++) // Generate moves for all other pieces (Only the current color's pieces)
            {
                PieceName piece = (PieceName)i;
                BitBoard pieceBitboard = boardState.GetBitboard(piece);
                moves.AddRange(GenerateMovesForPiece(piece, pieceBitboard));
            }

            return moves;
        }

        List<Move> GenerateMovesForPiece(PieceName piece, BitBoard bitboard)
        {
            List<Move> moves = [];
            return piece switch
            {
                PieceName.WhitePawn => GeneratePawnMoves(bitboard, true),
                PieceName.WhiteKnight => GenerateKnightMoves(bitboard),
                PieceName.WhiteBishop => GenerateBishopMoves(bitboard),
                PieceName.WhiteRook => GenerateRookMoves(bitboard),
                PieceName.WhiteQueen => GenerateQueenMoves(bitboard),
                PieceName.WhiteKing => GenerateKingMoves(bitboard),
                PieceName.BlackPawn => GeneratePawnMoves(bitboard, false),
                PieceName.BlackKnight => GenerateKnightMoves(bitboard),
                PieceName.BlackBishop => GenerateBishopMoves(bitboard),
                PieceName.BlackRook => GenerateRookMoves(bitboard),
                PieceName.BlackQueen => GenerateQueenMoves(bitboard),
                PieceName.BlackKing => GenerateKingMoves(bitboard),
                _ => moves
            };
        }

        List<Move> GeneratePawnMoves(BitBoard bitboard, bool isWhite) // In the move string representation the pawn does not need to be specified (hence the '\0' for movedPiece)
        {
            List<Move> moves = [];
            int[] captureOffsets = isWhite ? [7, 9] : [-9, -7];

            // Double push
            int offset = isWhite ? 16 : -16;
            int singleOffset = isWhite ? 8 : -8;
            BitBoard availablePawns = bitboard & (isWhite ? BitBoard.RANK_2 : BitBoard.RANK_7);
            while (availablePawns != 0)
            {
                int startSquare = BitBoard.PopLSB(ref availablePawns);
                int targetSquare = startSquare + offset;
                int pathSquare = startSquare + singleOffset;
                BitBoard pathBitboard = 1UL << targetSquare | (1UL << pathSquare);
                if ((boardState.AllPieces & pathBitboard) == 0) // Check if the path is clear
                {
                    moves.Add(new Move(startSquare, targetSquare, false, PieceType.Pawn));
                }
            }

            // A method to add moves to the list considering promotion
            void AddMoves(ref List<Move> moves, int startSquare, int targetSquare, bool isCapture, char checkStatus = '\0')
            {
                bool promotionCondition = isWhite ? ((1UL << targetSquare & BitBoard.RANK_8) != 0) : ((1UL << targetSquare & BitBoard.RANK_1) != 0);
                if (promotionCondition)
                {
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Knight));
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Bishop));
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Rook));
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Queen));
                }
                else
                {
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn));
                }
            }

            // Single push
            offset = isWhite ? 8 : -8;
            availablePawns = bitboard;
            while (availablePawns != 0)
            {
                int startSquare = BitBoard.PopLSB(ref availablePawns);
                int targetSquare = startSquare + offset;
                if ((boardState.AllPieces & (1UL << targetSquare)) == 0) // Check if the target square is empty
                {
                    AddMoves(ref moves, startSquare, targetSquare, false);
                }
            }

            // Captures and en passant
            availablePawns = bitboard;
            while (availablePawns != 0)
            {
                int startSquare = BitBoard.PopLSB(ref availablePawns);
                foreach (int captureOffset in captureOffsets)
                {
                    int targetSquare = startSquare + captureOffset;
                    int startFile = startSquare % 8;
                    int targetFile = targetSquare % 8;
                    if (Math.Abs(targetFile - startFile) != 1) continue; // Ensure the capture does not wrap around the board

                    if ((boardState.EnemyPieces & (1UL << targetSquare)) != 0) // Check if the target square has an enemy piece
                    {
                        AddMoves(ref moves, startSquare, targetSquare, true);
                    }
                    else if (boardState.EnPassantSquare == targetSquare)
                    {
                        moves.Add(new Move(startSquare, targetSquare, false, PieceType.Pawn, PromotionType.None, MoveType.EnPassant));
                    }
                }
            }

            return moves;
        }

        List<Move> GenerateKnightMoves(BitBoard bitboard)
        {
            List<Move> moves = [];
            int startSquare;
            while ((startSquare = BitBoard.PopLSB(ref bitboard)) != -1)
            {
                BitBoard knightMoves = magics.GetKnightMoves(startSquare);
                knightMoves &= ~boardState.FriendlyPieces;

                while (knightMoves != 0)
                {
                    int targetSquare = BitBoard.PopLSB(ref knightMoves);
                    BitBoard targetMask = 1UL << targetSquare;
                    bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Knight));
                }
            }
            return moves;
        }

        List<Move> GenerateBishopMoves(BitBoard bitboard)
        {
            List<Move> moves = [];
            int startSquare;
            while ((startSquare = BitBoard.PopLSB(ref bitboard)) != -1)
            {
                BitBoard bishopMoves = magics.GetBishopAttacks(startSquare, boardState.AllPieces.ToU64());
                bishopMoves &= ~boardState.FriendlyPieces;

                while (bishopMoves != 0)
                {
                    int targetSquare = BitBoard.PopLSB(ref bishopMoves);
                    BitBoard targetMask = 1UL << targetSquare;
                    bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Bishop));
                }
            }
            return moves;
        }

        List<Move> GenerateRookMoves(BitBoard bitboard)
        {
            List<Move> moves = [];
            int startSquare;
            while ((startSquare = BitBoard.PopLSB(ref bitboard)) != -1)
            {
                BitBoard rookMoves = magics.GetRookAttacks(startSquare, boardState.AllPieces.ToU64());
                rookMoves &= ~boardState.FriendlyPieces;

                while (rookMoves != 0)
                {
                    int targetSquare = BitBoard.PopLSB(ref rookMoves);
                    BitBoard targetMask = 1UL << targetSquare;
                    bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Rook));
                }
            }
            return moves;
        }

        List<Move> GenerateQueenMoves(BitBoard bitboard)
        {
            List<Move> moves = [.. GenerateBishopMoves(bitboard), .. GenerateRookMoves(bitboard)];
            return moves;
        }

        List<Move> GenerateKingMoves(BitBoard bitboard)
        {
            List<Move> moves = [];
            int startSquare = BitBoard.PopLSB(ref bitboard);
            BitBoard kingMoves = magics.GetKingMoves(startSquare);
            kingMoves &= ~boardState.FriendlyPieces;

            while (kingMoves != 0)
            {
                int targetSquare = BitBoard.PopLSB(ref kingMoves);
                BitBoard targetMask = 1UL << targetSquare;
                bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.King));
            }

            // Castling moves
            bool CanCastle(CastlingRightsIndex castlingRight, int startIndex)
            {
                bool isShortCastle = castlingRight == CastlingRightsIndex.WhiteKingSide || castlingRight == CastlingRightsIndex.BlackKingSide;
                int rookSquare = startIndex + (isShortCastle ? 3 : -4);
                int currentSquare = startIndex + (isShortCastle ? 1 : -1);
                while (currentSquare != rookSquare)
                {
                    if ((boardState.AllPieces & (1UL << currentSquare)) != 0)
                    {
                        return false; // There is a piece blocking the path
                    }
                    currentSquare += isShortCastle ? 1 : -1; // Move towards the rook
                }

                return true;
            }


            bool isWhite = CurrentColor == SideColor.White;
            CastlingRightsIndex shortCastlingIndex = isWhite ? CastlingRightsIndex.WhiteKingSide : CastlingRightsIndex.BlackKingSide;
            CastlingRightsIndex longCastlingIndex = isWhite ? CastlingRightsIndex.WhiteQueenSide : CastlingRightsIndex.BlackQueenSide;
            if (boardState.IsCastlingRightAvailable(shortCastlingIndex) && CanCastle(shortCastlingIndex, startSquare))
            {
                moves.Add(new Move(startSquare, startSquare + 2, false, PieceType.King, PromotionType.None, MoveType.ShortCastle)); // Short castle is always to the right (+2)
            }
            if (boardState.IsCastlingRightAvailable(longCastlingIndex) && CanCastle(longCastlingIndex, startSquare))
            {
                moves.Add(new Move(startSquare, startSquare - 2, false, PieceType.King, PromotionType.None, MoveType.LongCastle)); // Long castle is always to the left (-2)
            }

            return moves;
        }


    }
}