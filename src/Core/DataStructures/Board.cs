using Core.Helpers;

namespace Core.DataStructures;

public readonly struct Board
{
    /*
        A Board object represents the state of a chess game.
        Contains the 12 bitboards, the 64 squares, the side to move, the en passant square, and the castling rights.
        The bitboard indexes are determined by the PieceName enum, while the castling rights are determined by the CastlingRightsIndex enum.
        Remember: all calculations on a board are given from the point of view of the playing side.
    */
    public readonly ulong[] bitboards;
    public readonly char[] squares;
    public readonly SideColor sideToMove;
    public readonly int enPassantSquare;
    public readonly bool[] castlingRights;
    public readonly int halfmoveClock;
    public readonly int fullmoveNumber;
    public readonly ulong zobristHash;

    // Methods to get the desired bitboard
    public ulong GetBitboard(PieceName pieceName) => bitboards[(int)pieceName];
    public ulong GetBitboard(PieceType pieceType, SideColor sideColor) => bitboards[(int)pieceType + (int)sideColor * 6];
    public ulong GetSideBitboard(SideColor color)
    {
        ulong sideBitboard = 0;

        for (int i = 6 * (int)color; i < 6 + (int)color; i++)
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
    // Constructors

    public Board()
    {
        this = new Board("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
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

        this = new Board(
            bitboards,
            pieceArray,
            isWhiteToMove ? SideColor.White : SideColor.Black,
            enPassantTarget,
            castlingRightsArray,
            halfMoveClock,
            fullMoveNumber
        );
    }

    public Board(ulong[] bitboards, char[] squares, SideColor sideToMove, int enPassantSquare, bool[] castlingRights, int halfmoveClock = 0, int fullmoveNumber = 1)
    {
        // Constructor that computes the Zobrist hash based on the provided board state
        ulong zobristHash = ZobristHasher.ComputeHash(
            squares,
            sideToMove,
            enPassantSquare,
            castlingRights
        );

        this.bitboards = bitboards;
        this.squares = squares;
        this.sideToMove = sideToMove;
        this.enPassantSquare = enPassantSquare;
        this.castlingRights = castlingRights;
        this.halfmoveClock = halfmoveClock;
        this.fullmoveNumber = fullmoveNumber;
        this.zobristHash = zobristHash;
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

}