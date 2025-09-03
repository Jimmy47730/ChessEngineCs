using Core.Board;

namespace Core.Helpers;

public static class FenUtility
{
    public const string defaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public static BoardState ReadFen(string fen = defaultFen)
    {
        BoardState boardState;
        string[] parts = fen.Split(' ');

        string[] rows = parts[0].Split('/');
        bool isWhiteToMove = parts[1] == "w";
        string castlingRights = parts[2];
        int enPassantTarget = Square.Index(parts[3]);
        int halfMoveClock = int.Parse(parts[4]);
        int fullMoveNumber = int.Parse(parts[5]);
        char[] pieceArray = ParsePieces(rows);
        bool[] castlingRightsArray = ParseCastlingRights(castlingRights);
        BitBoard[] bitboards = BoardHelper.CreateBitboards(pieceArray);

        boardState = new BoardState(
            bitboards,
            pieceArray,
            halfMoveClock,
            fullMoveNumber,
            enPassantTarget,
            castlingRightsArray,
            isWhiteToMove ? SideColor.White : SideColor.Black);

        return boardState;
    }

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


    public static string WriteFen(BoardState board)
    {
        string fen = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < 8; file++)
            {
                int square = Square.Index(rank, file);
                char piece = board.GetPieceAtSquare(square);

                if (piece == '\0') empty++;
                else
                {
                    if (empty > 0)
                    {
                        fen += empty.ToString();
                        empty = 0;
                    }
                    fen += piece;
                }
            }
            fen += empty > 0 ? empty.ToString() : "";
            if (rank > 0) fen += "/";
        }
        fen += board.IsWhiteToMove ? " w " : " b ";
        string castling = "";
        if (board.IsCastlingRightAvailable(CastlingRightsIndex.WhiteKingSide)) castling += "K";
        if (board.IsCastlingRightAvailable(CastlingRightsIndex.WhiteQueenSide)) castling += "Q";
        if (board.IsCastlingRightAvailable(CastlingRightsIndex.BlackKingSide)) castling += "k";
        if (board.IsCastlingRightAvailable(CastlingRightsIndex.BlackQueenSide)) castling += "q";

        fen += castling == "" ? "- " : castling + " ";
        fen += board.EnPassantSquare == -1 ? "- " : Square.Name(board.EnPassantSquare) + " ";
        fen += board.HalfMoveClock + " ";
        fen += board.FullMoveNumber;
        
        return fen;
    }
}