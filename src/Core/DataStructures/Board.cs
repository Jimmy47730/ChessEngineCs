using Core.Helpers;

namespace Core.DataStructures;

public class Board
{
    /*
        A Board object represents the state of a chess game.
        Contains the 12 bitboards, the 64 squares, the side to move, the en passant square, and the castling rights.
        The bitboard indexes are determined by the PieceName enum, while the castling rights are determined by the CastlingRightsIndex enum.
        Remember: all calculations on a board are given from the point of view of the playing side.
    */
    private readonly ulong[] bitboards;
    private readonly char[] squares;
    private readonly bool[] castlingRights;
    public SideColor sideToMove { get; private set; }
    public int enPassantSquare { get; private set; }
    public int halfmoveClock { get; private set; }
    public int fullmoveNumber { get; private set; }
    public ulong zobristHash { get; private set; }

    // Methods to get the desired bitboard
    public ulong GetBitboard(PieceName pieceName) => bitboards[(int)pieceName];
    public ulong GetBitboard(int pieceIndex) => bitboards[pieceIndex];
    public ulong GetBitboard(PieceType pieceType, SideColor sideColor) => bitboards[(int)pieceType + (int)sideColor * 6];
    public char GetPieceAt(int square) => squares[square];
    public bool HasCastlingRight(CastlingRightsIndex right) => castlingRights[(int)right];
    public bool[] GetCastlingRights() => (bool[])castlingRights.Clone();

    public ulong GetSideBitboard(SideColor color)
    {
        ulong sideBitboard = 0;

        int firstPiece = 6 * (int)color;
        int endPiece = firstPiece + 6;
        for (int i = firstPiece; i < endPiece; i++)
        {
            sideBitboard |= bitboards[i];
        }

        return sideBitboard;
    }
    public ulong GetFullBitboard()
    {
        ulong fullBitboard = 0;

        for (int i = 0; i < 12; i++)
        {
            fullBitboard |= bitboards[i];
        }

        return fullBitboard;
    }

    public SideColor ChangeSideToMove(SideColor newSide)
    {
        sideToMove = newSide;
        return 1 - sideToMove;
    }
    // Constructors

    public Board() : this("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
    }

    public Board(string fen)
    {
        // Implementation for parsing FEN string and initializing the board state

        string[] parts = fen.Split(' ');

        string[] rows = parts[0].Split('/');
        bool isWhiteToMove = parts[1] == "w";
        string castlingRights = parts[2];
        int enPassantTarget = StringRepr.SquareConversion(parts[3]);
        int halfMoveClock = int.Parse(parts[4]);
        int fullMoveNumber = int.Parse(parts[5]);
        char[] pieceArray = ParsePieces(rows);
        bool[] castlingRightsArray = ParseCastlingRights(castlingRights);
        ulong[] bitboards = CreateBitboards(pieceArray);

        this.bitboards = bitboards;
        this.squares = pieceArray;
        this.sideToMove = isWhiteToMove ? SideColor.White : SideColor.Black;
        this.enPassantSquare = enPassantTarget;
        this.castlingRights = castlingRightsArray;
        this.halfmoveClock = halfMoveClock;
        this.fullmoveNumber = fullMoveNumber;
        this.zobristHash = ZobristHasher.ComputeHash(
            squares,
            sideToMove,
            enPassantSquare,
            castlingRightsArray
        );
    }

    public Board(ulong[] bitboards, char[] squares, SideColor sideToMove, int enPassantSquare, bool[] castlingRights, int halfmoveClock = 0, int fullmoveNumber = 1)
    {
        // Constructor that computes the Zobrist hash based on the provided board state
        if (bitboards.Length != 12)
            throw new ArgumentException("The board must contain 12 bitboards.", nameof(bitboards));
        if (squares.Length != 64)
            throw new ArgumentException("The board must contain exactly 64 squares.", nameof(squares));
        if (castlingRights.Length != 4)
            throw new ArgumentException("The board must contain 4 castling rights.", nameof(castlingRights));

        this.bitboards = (ulong[])bitboards.Clone();
        this.squares = (char[])squares.Clone();
        this.sideToMove = sideToMove;
        this.enPassantSquare = enPassantSquare;
        this.castlingRights = (bool[])castlingRights.Clone();
        this.halfmoveClock = halfmoveClock;
        this.fullmoveNumber = fullmoveNumber;
        this.zobristHash = ZobristHasher.ComputeHash(
            squares,
            sideToMove,
            enPassantSquare,
            this.castlingRights
        );
    }


    // Private helper methods for parsing FEN and creating bitboards
    private static char[] ParsePieces(string[] rows)
    {
        var boardArray = new char[64];
        for (int i = 0; i < 8; i++)
        {
            int row = 7 - i; // Reverse the row order
            int col = 0;
            foreach (char c in rows[i])
            {
                if (char.IsDigit(c))
                {
                    int empty = c - '0';
                    for (int j = 0; j < empty; j++)
                    {
                        boardArray[row * 8 + col] = '\0'; // Empty square
                        col++;
                    }
                }
                else
                {
                    boardArray[row * 8 + col] = c;
                    col++;
                }
            }
        }
        return boardArray;
    }
    private static bool[] ParseCastlingRights(string castlingRights)
    {
        bool[] rights = new bool[4]; // KQkq
        foreach (char c in castlingRights)
        {
            switch (c)
            {
                case 'K':
                    rights[0] = true; // White kingside
                    break;
                case 'Q':
                    rights[1] = true; // White queenside
                    break;
                case 'k':
                    rights[2] = true; // Black kingside
                    break;
                case 'q':
                    rights[3] = true; // Black queenside
                    break;
            }
        }
        return rights;
    }
    private static ulong[] CreateBitboards(char[] pieceArray)
    {
        var bitboards = new ulong[12];
        for (int i = 0; i < pieceArray.Length; i++)
        {
            char piece = pieceArray[i];
            if (piece != '\0')
            {
                int pieceIndex = (int)StringRepr.PieceConversion(piece);
                bitboards[pieceIndex] |= 1UL << i;
            }
        }
        return bitboards;
    }


    // Methods to apply/undo moves
    public UndoInfo ApplyMove(Move move)
    {
        if (!move.IsValid())
            throw new ArgumentException("Cannot apply an invalid move.", nameof(move));

        int from = move.From();
        int to = move.To();
        if (from == to)
            throw new ArgumentException("A move must change squares.", nameof(move));

        char movedPiece = squares[from];
        if (movedPiece == '\0')
            throw new ArgumentException("The move's source square is empty.", nameof(move));

        int movedPieceIndex = (int)StringRepr.PieceConversion(movedPiece);
        int capturedSquare = move.IsEnPassant() ? to + (sideToMove == SideColor.White ? -8 : 8) : to;
        if (capturedSquare < 0 || capturedSquare >= squares.Length)
            throw new ArgumentException("The move's capture square is outside the board.", nameof(move));

        char capturedPiece = squares[capturedSquare];
        if (move.IsEnPassant())
        {
            capturedPiece = sideToMove == SideColor.White ? 'p' : 'P';
            if (enPassantSquare != to || squares[to] != '\0' || squares[capturedSquare] != capturedPiece)
                throw new ArgumentException("The en passant move is inconsistent with the board.", nameof(move));
        }

        int rookStartSquare = -1;
        int rookEndSquare = -1;
        if (move.IsCastling())
        {
            if (from != (sideToMove == SideColor.White ? 4 : 60) ||
                to != from + (move.IsKingSideCastle() ? 2 : -2))
                throw new ArgumentException("The castling move has invalid king squares.", nameof(move));

            rookStartSquare = move.IsKingSideCastle() ? from + 3 : from - 4;
            rookEndSquare = move.IsKingSideCastle() ? from + 1 : from - 1;
            char rook = sideToMove == SideColor.White ? 'R' : 'r';
            if (squares[rookStartSquare] != rook || squares[rookEndSquare] != '\0')
                throw new ArgumentException("The castling move is inconsistent with the board.", nameof(move));
        }

        if (move.IsPromotion() &&
            ((PieceType)(movedPieceIndex % 6) != PieceType.Pawn ||
             move.PromotionPieceType() == PieceType.None ||
             (to / 8 != 0 && to / 8 != 7)))
            throw new ArgumentException("The promotion move is inconsistent with the board.", nameof(move));

        int previousCastlingRights = 0;
        for (int i = 0; i < castlingRights.Length; i++)
        {
            if (castlingRights[i])
                previousCastlingRights |= 1 << i;
        }

        UndoInfo undoInfo = new(
            capturedPiece,
            capturedSquare,
            enPassantSquare,
            previousCastlingRights,
            halfmoveClock,
            fullmoveNumber,
            sideToMove,
            zobristHash
        );

        bitboards[movedPieceIndex] = Operations.ClearBit(bitboards[movedPieceIndex], from);
        if (capturedPiece != '\0')
        {
            int capturedPieceIndex = (int)StringRepr.PieceConversion(capturedPiece);
            bitboards[capturedPieceIndex] = Operations.ClearBit(bitboards[capturedPieceIndex], capturedSquare);
            squares[capturedSquare] = '\0';
        }

        squares[from] = '\0';
        if (move.IsPromotion())
        {
            int promotedPieceIndex = (int)move.PromotionPieceType() + 6 * (int)sideToMove;
            bitboards[promotedPieceIndex] = Operations.SetBit(bitboards[promotedPieceIndex], to);
            squares[to] = StringRepr.PieceConversion(move.PromotionPieceType());
            if (sideToMove == SideColor.Black)
                squares[to] = char.ToLowerInvariant(squares[to]);
        }
        else
        {
            bitboards[movedPieceIndex] = Operations.SetBit(bitboards[movedPieceIndex], to);
            squares[to] = movedPiece;
        }

        if (move.IsCastling())
        {
            char rook = sideToMove == SideColor.White ? 'R' : 'r';
            int rookIndex = (int)PieceName.WhiteRook + 6 * (int)sideToMove;
            bitboards[rookIndex] = Operations.ClearBit(bitboards[rookIndex], rookStartSquare);
            bitboards[rookIndex] = Operations.SetBit(bitboards[rookIndex], rookEndSquare);
            squares[rookStartSquare] = '\0';
            squares[rookEndSquare] = rook;
        }

        UpdateCastlingRights(from, to, movedPiece, capturedPiece);
        enPassantSquare = move.IsDoublePawnPush() ? (from + to) / 2 : -1;
        halfmoveClock = (movedPieceIndex % 6 == (int)PieceType.Pawn || capturedPiece != '\0') ? 0 : halfmoveClock + 1;
        if (sideToMove == SideColor.Black)
            fullmoveNumber++;
        sideToMove = sideToMove == SideColor.White ? SideColor.Black : SideColor.White;
        zobristHash = ZobristHasher.ComputeHash(squares, sideToMove, enPassantSquare, castlingRights);

        return undoInfo;
    }

    public void UndoMove(Move move, UndoInfo undoInfo)
    {
        if (!move.IsValid())
            throw new ArgumentException("Cannot undo an invalid move.", nameof(move));

        int from = move.From();
        int to = move.To();
        char movedPiece = move.IsPromotion()
            ? (undoInfo.PreviousSideToMove == SideColor.White ? 'P' : 'p')
            : squares[to];
        int movedPieceIndex = (int)StringRepr.PieceConversion(movedPiece);

        if (move.IsCastling())
        {
            int rookStartSquare = move.IsKingSideCastle() ? from + 3 : from - 4;
            int rookEndSquare = move.IsKingSideCastle() ? from + 1 : from - 1;
            int rookIndex = (int)PieceName.WhiteRook + 6 * (int)undoInfo.PreviousSideToMove;
            char rook = undoInfo.PreviousSideToMove == SideColor.White ? 'R' : 'r';

            bitboards[rookIndex] = Operations.ClearBit(bitboards[rookIndex], rookEndSquare);
            bitboards[rookIndex] = Operations.SetBit(bitboards[rookIndex], rookStartSquare);
            squares[rookEndSquare] = '\0';
            squares[rookStartSquare] = rook;
        }

        int pieceAtDestinationIndex = move.IsPromotion()
            ? (int)move.PromotionPieceType() + 6 * (int)undoInfo.PreviousSideToMove
            : movedPieceIndex;
        bitboards[pieceAtDestinationIndex] = Operations.ClearBit(bitboards[pieceAtDestinationIndex], to);
        bitboards[movedPieceIndex] = Operations.SetBit(bitboards[movedPieceIndex], from);
        squares[to] = '\0';
        squares[from] = movedPiece;

        if (undoInfo.CapturedPiece != '\0')
        {
            int capturedPieceIndex = (int)StringRepr.PieceConversion(undoInfo.CapturedPiece);
            bitboards[capturedPieceIndex] = Operations.SetBit(bitboards[capturedPieceIndex], undoInfo.CapturedSquare);
            squares[undoInfo.CapturedSquare] = undoInfo.CapturedPiece;
        }

        for (int i = 0; i < castlingRights.Length; i++)
            castlingRights[i] = (undoInfo.PreviousCastlingRights & (1 << i)) != 0;

        sideToMove = undoInfo.PreviousSideToMove;
        enPassantSquare = undoInfo.PreviousEnPassantSquare;
        halfmoveClock = undoInfo.PreviousHalfmoveClock;
        fullmoveNumber = undoInfo.PreviousFullmoveNumber;
        zobristHash = undoInfo.PreviousZobristHash;
    }

    private void UpdateCastlingRights(int from, int to, char movedPiece, char capturedPiece)
    {
        if (char.ToUpperInvariant(movedPiece) == 'K')
        {
            castlingRights[2 * (int)sideToMove] = false;
            castlingRights[2 * (int)sideToMove + 1] = false;
        }

        if (char.ToUpperInvariant(movedPiece) == 'R')
            UpdateRookCastlingRight(from);
        if (char.ToUpperInvariant(capturedPiece) == 'R')
            UpdateRookCastlingRight(to);

        void UpdateRookCastlingRight(int square)
        {
            switch (square)
            {
                case 0: castlingRights[(int)CastlingRightsIndex.WhiteQueenSide] = false; break;
                case 7: castlingRights[(int)CastlingRightsIndex.WhiteKingSide] = false; break;
                case 56: castlingRights[(int)CastlingRightsIndex.BlackQueenSide] = false; break;
                case 63: castlingRights[(int)CastlingRightsIndex.BlackKingSide] = false; break;
            }
        }
    }
}