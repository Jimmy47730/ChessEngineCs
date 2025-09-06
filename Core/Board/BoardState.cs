using Core.Helpers;

namespace Core.Board
{
    public readonly struct BoardState
    {
        /*
            Represents the state of the chess board, including the positions of all pieces.
            The board is an 8x8 grid, and each piece is represented by its type and color.
            The state includes information about which player's turn it is, as well as any special
            conditions (e.g., check, checkmate, stalemate).

            The pieces positions are represented as 12 different bitboards, one for each piece type and color, and an array of chars representing the whole board.
        */

        // Bitboards and piece array
        private readonly BitBoard whitePawns, whiteKnights, whiteBishops, whiteRooks, whiteQueens, whiteKing;
        private readonly BitBoard blackPawns, blackKnights, blackBishops, blackRooks, blackQueens, blackKing;
        private readonly char[] pieceArray;

        // Hash key
        private readonly ulong hashKey;

        // Game properties
        private readonly int halfMoveClock;
        private readonly int fullMoveNumber;
        private readonly int enPassantSquare;
        private readonly bool[] castlingRights;
        private readonly SideColor sideToMove;

        // Constructor
        public BoardState(BitBoard[] bitboards, char[] pieceArray, int halfMoveClock, int fullMoveNumber,
                        int enPassantSquare, bool[] castlingRights, SideColor sideToMove)
        {
            this.whitePawns = bitboards[0];
            this.whiteKnights = bitboards[1];
            this.whiteBishops = bitboards[2];
            this.whiteRooks = bitboards[3];
            this.whiteQueens = bitboards[4];
            this.whiteKing = bitboards[5];
            this.blackPawns = bitboards[6];
            this.blackKnights = bitboards[7];
            this.blackBishops = bitboards[8];
            this.blackRooks = bitboards[9];
            this.blackQueens = bitboards[10];
            this.blackKing = bitboards[11];

            this.hashKey = 0UL;

            if (pieceArray == null || pieceArray.Length != 64)
                throw new ArgumentException("pieceArray must have exactly 64 elements.", nameof(pieceArray));
            this.pieceArray = pieceArray;

            this.halfMoveClock = halfMoveClock;
            this.fullMoveNumber = fullMoveNumber;
            this.enPassantSquare = enPassantSquare;
            this.castlingRights = castlingRights;
            this.sideToMove = sideToMove;

            this.hashKey = ZobristHasher.ComputeHash(this);
        }

        public BoardState(BoardState other)
        {
            this = other;
        }

        public BoardState(string fen)
        {
            this = new BoardState(FenUtility.ReadFen(fen));
        }
        // Properties
        public readonly BitBoard WhitePawns => whitePawns;
        public readonly BitBoard WhiteKnights => whiteKnights;
        public readonly BitBoard WhiteBishops => whiteBishops;
        public readonly BitBoard WhiteRooks => whiteRooks;
        public readonly BitBoard WhiteQueens => whiteQueens;
        public readonly BitBoard WhiteKing => whiteKing;
        public readonly BitBoard BlackPawns => blackPawns;
        public readonly BitBoard BlackKnights => blackKnights;
        public readonly BitBoard BlackBishops => blackBishops;
        public readonly BitBoard BlackRooks => blackRooks;
        public readonly BitBoard BlackQueens => blackQueens;
        public readonly BitBoard BlackKing => blackKing;
        public readonly BitBoard WhitePieces => whitePawns | whiteKnights | whiteBishops | whiteRooks | whiteQueens | whiteKing;
        public readonly BitBoard BlackPieces => blackPawns | blackKnights | blackBishops | blackRooks | blackQueens | blackKing;
        public readonly BitBoard AllPieces => WhitePieces | BlackPieces;
        public readonly BitBoard FriendlyPieces => sideToMove == SideColor.White ? WhitePieces : BlackPieces;
        public readonly BitBoard EnemyPieces => sideToMove == SideColor.White ? BlackPieces : WhitePieces;
        public readonly BitBoard[] Bitboards =>
        [
            whitePawns, whiteKnights, whiteBishops, whiteRooks, whiteQueens, whiteKing,
            blackPawns, blackKnights, blackBishops, blackRooks, blackQueens, blackKing
        ];


        public readonly char[] PieceArray => pieceArray;
        public readonly ulong HashKey => hashKey;
        public readonly int HalfMoveClock => halfMoveClock;
        public readonly int FullMoveNumber => fullMoveNumber;
        public readonly int EnPassantSquare => enPassantSquare;
        public readonly bool[] CastlingRights => castlingRights;
        public readonly bool IsWhiteToMove => sideToMove == SideColor.White;
        public readonly SideColor SideToMove => sideToMove;

        // Methods
        public readonly BitBoard GetBitboard(SideColor color, PieceType pieceType)
        {
            return (color, pieceType) switch
            {
                (SideColor.White, PieceType.Pawn) => whitePawns,
                (SideColor.White, PieceType.Knight) => whiteKnights,
                (SideColor.White, PieceType.Bishop) => whiteBishops,
                (SideColor.White, PieceType.Rook) => whiteRooks,
                (SideColor.White, PieceType.Queen) => whiteQueens,
                (SideColor.White, PieceType.King) => whiteKing,
                (SideColor.Black, PieceType.Pawn) => blackPawns,
                (SideColor.Black, PieceType.Knight) => blackKnights,
                (SideColor.Black, PieceType.Bishop) => blackBishops,
                (SideColor.Black, PieceType.Rook) => blackRooks,
                (SideColor.Black, PieceType.Queen) => blackQueens,
                (SideColor.Black, PieceType.King) => blackKing,
                _ => BitBoard.Empty
            };
        }

        public readonly BitBoard GetBitboard(PieceName pieceName)
        {
            return pieceName switch
            {
                PieceName.WhitePawn => whitePawns,
                PieceName.WhiteKnight => whiteKnights,
                PieceName.WhiteBishop => whiteBishops,
                PieceName.WhiteRook => whiteRooks,
                PieceName.WhiteQueen => whiteQueens,
                PieceName.WhiteKing => whiteKing,
                PieceName.BlackPawn => blackPawns,
                PieceName.BlackKnight => blackKnights,
                PieceName.BlackBishop => blackBishops,
                PieceName.BlackRook => blackRooks,
                PieceName.BlackQueen => blackQueens,
                PieceName.BlackKing => blackKing,
                _ => BitBoard.Empty
            };
        }
        public readonly bool IsSquareOccupied(int square) => pieceArray[square] != '\0';
        public readonly char GetPieceAtSquare(int square) => pieceArray[square];
        public readonly bool IsCastlingRightAvailable(CastlingRightsIndex index) => castlingRights[(int)index];

        // Apply move
        public BoardState ApplyMove(Move move)
        {
            BoardState boardState = this;
            int fromSquare = move.FromSquare;
            int toSquare = move.ToSquare;
            PromotionType promotion = move.PromotedPiece;
            PieceName pieceMoved = BoardHelper.GetPieceName(boardState.GetPieceAtSquare(fromSquare));
            if (pieceMoved == PieceName.None)
                throw new ArgumentException($"No piece found at fromSquare {fromSquare} for move {move}. PieceName: {pieceMoved}. BoardState: \n{boardState}");
            PieceName pieceCaptured = boardState.GetPieceAtSquare(toSquare) != '\0' ? BoardHelper.GetPieceName(boardState.GetPieceAtSquare(toSquare)) : PieceName.None;
            bool isWhite = pieceMoved < PieceName.BlackPawn;

            // IMPORTANT: BoardState is a struct holding references to arrays. To keep immutability semantics
            // we must clone the underlying arrays before mutating, otherwise earlier states (e.g. root position
            // reused in perft) get unintentionally modified causing later moves to fail (e.g. missing pawn at a2).
            BitBoard[] bitboards = boardState.Bitboards; // This already returns a fresh array copy of value type BitBoards
            char[] pieceArray = (char[])boardState.PieceArray.Clone();
            bool[] castlingRights = (bool[])boardState.CastlingRights.Clone();

            // Index validation
            void ValidateSquare(int square, string name)
            {
                if (square < 0 || square >= 64)
                    throw new IndexOutOfRangeException($"Invalid {name} index: {square} in ApplyMove. Move: {move}, Piece: {pieceMoved}");
            }
            ValidateSquare(fromSquare, nameof(fromSquare));
            ValidateSquare(toSquare, nameof(toSquare));

            // Before move processing - store the piece character (works on cloned array)
            char pieceChar = pieceArray[fromSquare];
            pieceArray[fromSquare] = '\0';

            if (move.IsPromotion)
            {
                // Remove the pawn
                bitboards[(int)pieceMoved] = bitboards[(int)pieceMoved].ClearBit(fromSquare);
                PieceName promotedPiece = promotion switch
                {
                    PromotionType.Knight => isWhite ? PieceName.WhiteKnight : PieceName.BlackKnight,
                    PromotionType.Bishop => isWhite ? PieceName.WhiteBishop : PieceName.BlackBishop,
                    PromotionType.Rook => isWhite ? PieceName.WhiteRook : PieceName.BlackRook,
                    PromotionType.Queen => isWhite ? PieceName.WhiteQueen : PieceName.BlackQueen,
                    _ => throw new ArgumentException("Invalid promotion type")
                };
                bitboards[(int)promotedPiece] = bitboards[(int)promotedPiece].SetBit(toSquare);
                pieceArray[toSquare] = BoardHelper.GetPieceChar(promotedPiece);
            }
            else
            {
                pieceArray[toSquare] = pieceChar; // Use the stored character
                // Update moved piece bitboard (must assign result back!)
                bitboards[(int)pieceMoved] = bitboards[(int)pieceMoved].ClearBit(fromSquare).SetBit(toSquare);
            }

            if (move.IsCapture && !move.IsEnPassant && pieceCaptured != PieceName.None)
            {
                bitboards[(int)pieceCaptured] = bitboards[(int)pieceCaptured].ClearBit(toSquare);
            }

            if (move.IsCastling)
            {
                int rookFromSquare = move.SpecialMove == MoveType.ShortCastle ? fromSquare + 3 : fromSquare - 4;
                int rookToSquare = move.SpecialMove == MoveType.ShortCastle ? toSquare - 1 : toSquare + 1;
                ValidateSquare(rookFromSquare, nameof(rookFromSquare));
                ValidateSquare(rookToSquare, nameof(rookToSquare));
                PieceName rookPiece = isWhite ? PieceName.WhiteRook : PieceName.BlackRook;
                bitboards[(int)rookPiece] = bitboards[(int)rookPiece].ClearBit(rookFromSquare).SetBit(rookToSquare);
                pieceArray[rookToSquare] = pieceArray[rookFromSquare];
                pieceArray[rookFromSquare] = '\0';
            }
            if (move.IsEnPassant)
            {
                int capturedPawnSquare = isWhite ? toSquare - 8 : toSquare + 8;
                ValidateSquare(capturedPawnSquare, nameof(capturedPawnSquare));
                PieceName capturedPawnPiece = isWhite ? PieceName.BlackPawn : PieceName.WhitePawn;
                bitboards[(int)capturedPawnPiece] = bitboards[(int)capturedPawnPiece].ClearBit(capturedPawnSquare);
                pieceArray[capturedPawnSquare] = '\0';
            }

            // Update the half-move clock
            // If the move is a capture or a pawn move, reset the half-move clock
            int halfMoveClock = boardState.HalfMoveClock;
            if (move.IsCapture || pieceMoved == PieceName.WhitePawn || pieceMoved == PieceName.BlackPawn)
                halfMoveClock = 0;
            else
                halfMoveClock++;

            // Update the full move number
            int fullMoveNumber = boardState.FullMoveNumber;
            // NOTE: In FEN the full move number increments after Black's move. Existing logic incremented after White.
            // Keeping behaviour consistent with FEN spec here.
            if (!isWhite) // Increment after black's move
                fullMoveNumber++;

            // Update the en passant square
            int enPassantSquare = -1;
            if (pieceMoved == PieceName.WhitePawn && toSquare - fromSquare == 16)
                enPassantSquare = fromSquare + 8; // White pawn double move
            else if (pieceMoved == PieceName.BlackPawn && fromSquare - toSquare == 16)
                enPassantSquare = fromSquare - 8; // Black pawn double move

            // Update catling rights
            if (pieceMoved == PieceName.WhiteKing)
            {
                castlingRights[(int)CastlingRightsIndex.WhiteKingSide] = false;
                castlingRights[(int)CastlingRightsIndex.WhiteQueenSide] = false;
            }
            else if (pieceMoved == PieceName.BlackKing)
            {
                castlingRights[(int)CastlingRightsIndex.BlackKingSide] = false;
                castlingRights[(int)CastlingRightsIndex.BlackQueenSide] = false;
            }
            else if (pieceMoved == PieceName.WhiteRook)
            {
                if (fromSquare == 0) // A1
                    castlingRights[(int)CastlingRightsIndex.WhiteQueenSide] = false;
                else if (fromSquare == 7) // H1
                    castlingRights[(int)CastlingRightsIndex.WhiteKingSide] = false;
            }
            else if (pieceMoved == PieceName.BlackRook)
            {
                if (fromSquare == 56) // A8
                    castlingRights[(int)CastlingRightsIndex.BlackQueenSide] = false;
                else if (fromSquare == 63) // H8
                    castlingRights[(int)CastlingRightsIndex.BlackKingSide] = false;
            }


            return new BoardState
            (
                bitboards,
                pieceArray,
                halfMoveClock,
                fullMoveNumber,
                enPassantSquare,
                castlingRights,
                isWhite ? SideColor.Black : SideColor.White // Toggle the side to move
            );
        }

        public BoardState ApplyMove(string moveUci)
        {
            if (moveUci.Length < 4 || moveUci.Length > 5)
                throw new ArgumentException("Invalid UCI move format", nameof(moveUci));

            int fromSquare = Square.Index(moveUci[0..2]);
            int toSquare = Square.Index(moveUci[2..4]);
            PromotionType promotion = PromotionType.None;

            if (moveUci.Length == 5)
            {
                promotion = moveUci[4] switch
                {
                    'n' => PromotionType.Knight,
                    'b' => PromotionType.Bishop,
                    'r' => PromotionType.Rook,
                    'q' => PromotionType.Queen,
                    _ => throw new ArgumentException("Invalid promotion piece in UCI move", nameof(moveUci))
                };
            }

            PieceName pieceMoved = BoardHelper.GetPieceName(GetPieceAtSquare(fromSquare));
            if (pieceMoved == PieceName.None)
                throw new ArgumentException($"No piece found at fromSquare {fromSquare} for UCI move {moveUci}");

            bool isWhite = pieceMoved < PieceName.BlackPawn;
            PieceName pieceCaptured = GetPieceAtSquare(toSquare) != '\0' ? BoardHelper.GetPieceName(GetPieceAtSquare(toSquare)) : PieceName.None;

            MoveType specialMove = MoveType.Normal;
            if (pieceMoved == PieceName.WhiteKing || pieceMoved == PieceName.BlackKing)
            {
                if (toSquare - fromSquare == 2) specialMove = MoveType.ShortCastle;
                else if (fromSquare - toSquare == 2) specialMove = MoveType.LongCastle;
            }
            else if (pieceMoved == PieceName.WhitePawn || pieceMoved == PieceName.BlackPawn)
            {
                if (toSquare == EnPassantSquare && pieceCaptured == PieceName.None)
                    specialMove = MoveType.EnPassant;
            }

            CheckType checkType = CheckType.None; // Determining check/mate requires additional logic

            Move move = new(fromSquare, toSquare, pieceCaptured != PieceName.None || specialMove == MoveType.EnPassant, BoardHelper.GetPieceType(pieceMoved), promotion, specialMove, checkType);

            // Apply the move
            return ApplyMove(move);
        }


        // Equality overrides
        public static implicit operator ulong(BoardState state) => state.HashKey;
        public static bool operator ==(BoardState state1, BoardState state2) => state1.HashKey == state2.HashKey;
        public static bool operator !=(BoardState state1, BoardState state2) => state1.HashKey != state2.HashKey;

        public override bool Equals(object? obj)
        {
            if (obj is BoardState other)
                return this == other;
            return false;
        }
        public override int GetHashCode()
        {
            return hashKey.GetHashCode();
        }

        // To string override
        public override string ToString()
        {
            return StringRepr.BoardDiagram(this); 
        }
    }
}