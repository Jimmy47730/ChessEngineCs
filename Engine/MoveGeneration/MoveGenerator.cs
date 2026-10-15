using Core.Helpers;
using Core.Board;
using Chess;

namespace Engine.MoveGeneration
{
    public class MoveGenerator
    {
        private static MoveGenerator instance = new();
        private static readonly object lockObj = new();
        private readonly Magics magics;
        private BoardState boardState;
        private List<BitBoard> pinRays = [];
        private SideColor CurrentColor => boardState.SideToMove;

        // Constructor and Singleton Instance
        public static MoveGenerator Instance()
        {
            if (instance == null)
            {
                lock (lockObj)
                {
                    instance ??= new MoveGenerator();
                }
            }
            return instance;
        }

        private MoveGenerator()
        {
            magics = Magics.Instance();
        }

        public List<Move> GenerateMoves(BoardState boardState)
        {
            this.boardState = boardState;

            int kingSquare = (CurrentColor == SideColor.White ? boardState.WhiteKing : boardState.BlackKing).LsbIndex();
            pinRays = CheckRestriction.GetPinRays(boardState, kingSquare);

            List<Move> moves = [];

            PieceName kingPiece = CurrentColor == SideColor.White ? PieceName.WhiteKing : PieceName.BlackKing;
            BitBoard kingBitboard = boardState.GetBitboard(kingPiece);
            BitBoard unsafeKingSquares = CheckRestriction.GetUnsafeKingSquares(boardState, kingSquare, out BitBoard checkingPieces);
            BitBoard allowedDestinations = CheckRestriction.SquaresToBlock(checkingPieces, kingSquare);
            bool isDoubleCheck = checkingPieces.PopCount() > 1;

            moves.AddRange(GenerateKingMoves(kingBitboard, ~unsafeKingSquares));

            if (isDoubleCheck)
                return moves; // Only king moves are allowed in double check

            foreach (var piece in Enum.GetValues<PieceType>())
            {
                BitBoard pieceBitboard = boardState.GetBitboard(CurrentColor, piece);
                PieceName pieceName = BoardHelper.GetPieceName(CurrentColor, piece);
                moves.AddRange(GenerateMovesForPiece(pieceName, pieceBitboard, allowedDestinations));
            }

            return moves;
        }

        List<Move> GenerateMovesForPiece(PieceName piece, BitBoard bitboard, BitBoard allowedDestinations)
        {
            List<Move> moves = [];
            return piece switch
            {
                PieceName.WhitePawn => GeneratePawnMoves(bitboard, true, allowedDestinations),
                PieceName.WhiteKnight => GenerateKnightMoves(bitboard, allowedDestinations),
                PieceName.WhiteBishop => GenerateBishopMoves(bitboard, false, allowedDestinations),
                PieceName.WhiteRook => GenerateRookMoves(bitboard, false, allowedDestinations),
                PieceName.WhiteQueen => GenerateQueenMoves(bitboard, allowedDestinations),
                PieceName.WhiteKing => moves, // King moves are generated separately at the start
                PieceName.BlackPawn => GeneratePawnMoves(bitboard, false, allowedDestinations),
                PieceName.BlackKnight => GenerateKnightMoves(bitboard, allowedDestinations),
                PieceName.BlackBishop => GenerateBishopMoves(bitboard, false, allowedDestinations),
                PieceName.BlackRook => GenerateRookMoves(bitboard, false, allowedDestinations),
                PieceName.BlackQueen => GenerateQueenMoves(bitboard, allowedDestinations),
                PieceName.BlackKing => moves, // King moves are generated separately at the start
                _ => moves
            };
        }

        List<Move> GeneratePawnMoves(BitBoard bitboard, bool isWhite, BitBoard allowedDestinations) // In the move string representation the pawn does not need to be specified (hence the '\0' for movedPiece)
        {
            List<Move> moves = [];
            int[] captureOffsets = isWhite ? [7, 9] : [-9, -7];

            BitBoard availablePawns = bitboard & (isWhite ? BitBoard.RANK_2 : BitBoard.RANK_7);

            BitBoard fullyPinnedPawns = CheckRestriction.GetHorizontallyStuck(bitboard, pinRays);
            BitBoard pushPinnedPawns = CheckRestriction.GetDiagonallyStuck(bitboard, pinRays);
            BitBoard capturePinnedPawns = CheckRestriction.GetVerticallyStuck(bitboard, pinRays);

            bitboard &= ~fullyPinnedPawns;
            PawnPush(ref moves, bitboard & ~pushPinnedPawns, availablePawns & ~pushPinnedPawns, isWhite);

            // Captures and en passant
            availablePawns = bitboard & ~capturePinnedPawns;
            PawnCapture(ref moves, captureOffsets, availablePawns);

            return moves;

            // A local function to add moves to the list considering promotion
            void AddMoves(ref List<Move> moves, int startSquare, int targetSquare, bool isCapture, char checkStatus = '\0')
            {
                bool promotionCondition = isWhite ? ((1UL << targetSquare & BitBoard.RANK_8) != 0) : ((1UL << targetSquare & BitBoard.RANK_1) != 0);
                if (promotionCondition)
                {
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Queen));
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Rook));
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Knight));
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn, PromotionType.Bishop));
                }
                else
                {
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Pawn));
                }
            }

            // A local function to handle pawn pushes (single and double)
            void PawnPush(ref List<Move> moves, BitBoard bitboard, BitBoard availablePawns, bool isWhite)
            {

                // Double push
                int offset = isWhite ? 16 : -16;
                int singleOffset = isWhite ? 8 : -8;
                while (availablePawns != 0)
                {
                    int startSquare = BitBoard.PopLSB(ref availablePawns);
                    int targetSquare = startSquare + offset;
                    int pathSquare = startSquare + singleOffset;
                    BitBoard pathBitboard = 1UL << targetSquare | (1UL << pathSquare);
                    if ((boardState.AllPieces & pathBitboard) == 0) // Check if the path is clear
                    {
                        BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                        if (allowedDestinations == BitBoard.Full || (allowedDestinations & targetMask) != 0)
                        {
                            moves.Add(new Move(startSquare, targetSquare, false, PieceType.Pawn));
                        }
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
                        BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                        if (allowedDestinations == BitBoard.Full || (allowedDestinations & targetMask) != 0)
                        {
                            AddMoves(ref moves, startSquare, targetSquare, false);
                        }
                    }
                }
            }

            // A local function to handle pawn captures and en passant
            void PawnCapture(ref List<Move> moves, int[] captureOffsets, BitBoard availablePawns)
            {
                while (availablePawns != 0)
                {
                    int startSquare = BitBoard.PopLSB(ref availablePawns);
                    foreach (int captureOffset in captureOffsets)
                    {
                        int targetSquare = startSquare + captureOffset;
                        int startFile = startSquare % 8;
                        int targetFile = targetSquare % 8;
                        if (Math.Abs(targetFile - startFile) != 1) continue; // Ensure the capture does not wrap around the board

                        BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                        if ((boardState.EnemyPieces & targetMask) != 0) // Check if the target square has an enemy piece
                        {
                            if (allowedDestinations == BitBoard.Full || (allowedDestinations & targetMask) != 0)
                            {
                                AddMoves(ref moves, startSquare, targetSquare, true);
                            }
                        }
                        else if (boardState.EnPassantSquare == targetSquare && (allowedDestinations == BitBoard.Full || (allowedDestinations & targetMask) != 0))
                        {
                            moves.Add(new Move(startSquare, targetSquare, false, PieceType.Pawn, PromotionType.None, MoveType.EnPassant));
                        }
                    }
                }
            }
        }

        List<Move> GenerateKnightMoves(BitBoard bitboard, BitBoard allowedDestinations)
        {
            List<Move> moves = [];
            int startSquare;

            // Knights cannot move if pinned
            bitboard &= ~CheckRestriction.GetRestrictedPieces(bitboard, pinRays);

            while ((startSquare = BitBoard.PopLSB(ref bitboard)) != -1)
            {
                BitBoard knightMoves = magics.GetKnightMoves(startSquare);
                knightMoves &= ~boardState.FriendlyPieces;

                while (knightMoves != 0)
                {
                    int targetSquare = BitBoard.PopLSB(ref knightMoves);
                    BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                    if (allowedDestinations != BitBoard.Full && (allowedDestinations & targetMask) == 0)
                        continue;
                    bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                    moves.Add(new Move(startSquare, targetSquare, isCapture, PieceType.Knight));
                }
            }
            return moves;
        }

        List<Move> GenerateBishopMoves(BitBoard bitboard, bool isQueen = false, BitBoard allowedDestinations = default)
        {
            List<Move> moves = [];
            int startSquare;

            // Bishops cannot move if pinned along a rank or file
            bitboard &= ~CheckRestriction.GetHorizontallyStuck(bitboard, pinRays);
            bitboard &= ~CheckRestriction.GetVerticallyStuck(bitboard, pinRays);

            while ((startSquare = BitBoard.PopLSB(ref bitboard)) != -1)
            {
                BitBoard bishopMoves = magics.GetBishopAttacks(startSquare, boardState.AllPieces.ToU64());
                bishopMoves &= ~boardState.FriendlyPieces;
                bool isPinned = CheckRestriction.GetDiagonallyStuck(BitBoard.FromSquare(startSquare), pinRays) != 0;

                while (bishopMoves != 0)
                {
                    int targetSquare = BitBoard.PopLSB(ref bishopMoves);

                    if (isPinned && !BoardHelper.AreDiagonallyAligned(startSquare, targetSquare))
                        continue; // Skip moves that are not along the pin ray (no need to check for the direction of the diagonal, you cannot end on another pin ray square anyway)

                    BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                    if (allowedDestinations != BitBoard.Full && (allowedDestinations & targetMask) == 0)
                        continue;
                    bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                    moves.Add(new Move(startSquare, targetSquare, isCapture, isQueen ? PieceType.Queen : PieceType.Bishop));
                }
            }
            return moves;
        }

        List<Move> GenerateRookMoves(BitBoard bitboard, bool isQueen = false, BitBoard allowedDestinations = default)
        {
            List<Move> moves = [];
            int startSquare;

            // Rooks cannot move if pinned along a diagonal
            bitboard &= ~CheckRestriction.GetDiagonallyStuck(bitboard, pinRays);

            while ((startSquare = BitBoard.PopLSB(ref bitboard)) != -1)
            {
                BitBoard rookMoves = magics.GetRookAttacks(startSquare, boardState.AllPieces.ToU64());
                rookMoves &= ~boardState.FriendlyPieces;
                bool isVerticallyPinned = CheckRestriction.GetVerticallyStuck(BitBoard.FromSquare(startSquare), pinRays) != 0;
                bool isHorizontallyPinned = CheckRestriction.GetHorizontallyStuck(BitBoard.FromSquare(startSquare), pinRays) != 0;

                while (rookMoves != 0)
                {
                    int targetSquare = BitBoard.PopLSB(ref rookMoves);

                    if (isVerticallyPinned && !BoardHelper.AreVerticallyAligned(startSquare, targetSquare))
                        continue; // Skip moves that are not along the pin ray

                    else if (isHorizontallyPinned && !BoardHelper.AreHorizontallyAligned(startSquare, targetSquare))
                        continue; // Skip moves that are not along the pin ray

                    BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                    if (allowedDestinations != BitBoard.Full && (allowedDestinations & targetMask) == 0)
                        continue;
                    bool isCapture = (targetMask & boardState.EnemyPieces) != 0;
                    moves.Add(new Move(startSquare, targetSquare, isCapture, isQueen ? PieceType.Queen : PieceType.Rook));
                }
            }
            return moves;
        }

        List<Move> GenerateQueenMoves(BitBoard bitboard, BitBoard allowedDestinations)
        {
            List<Move> moves = [.. GenerateBishopMoves(bitboard, true, allowedDestinations), .. GenerateRookMoves(bitboard, true, allowedDestinations)];
            return moves;
        }

        List<Move> GenerateKingMoves(BitBoard bitboard, BitBoard allowedDestinations)
        {
            List<Move> moves = [];
            int startSquare = BitBoard.PopLSB(ref bitboard);
            BitBoard kingMoves = magics.GetKingMoves(startSquare);
            kingMoves &= ~boardState.FriendlyPieces;

            while (kingMoves != 0)
            {
                int targetSquare = BitBoard.PopLSB(ref kingMoves);
                BitBoard targetMask = BitBoard.FromSquare(targetSquare);
                if (allowedDestinations != BitBoard.Full && (allowedDestinations & targetMask) == 0)
                    continue;
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
                int targetSquare = startSquare + 2;
                if (allowedDestinations == BitBoard.Full || (allowedDestinations & BitBoard.FromSquare(targetSquare)) != 0)
                {
                    moves.Add(new Move(startSquare, targetSquare, false, PieceType.King, PromotionType.None, MoveType.ShortCastle)); // Short castle is always to the right (+2)
                }
            }
            if (boardState.IsCastlingRightAvailable(longCastlingIndex) && CanCastle(longCastlingIndex, startSquare))
            {
                int targetSquare = startSquare - 2;
                if (allowedDestinations == BitBoard.Full || (allowedDestinations & BitBoard.FromSquare(targetSquare)) != 0)
                {
                    moves.Add(new Move(startSquare, targetSquare, false, PieceType.King, PromotionType.None, MoveType.LongCastle)); // Long castle is always to the left (-2)
                }
            }

            return moves;
        }


    }
}