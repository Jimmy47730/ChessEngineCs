using Core.DataStructures;

namespace Core.Helpers
{
    public static class StringRepr
    {
        public static int SquareConversion(string squareName)
        {
            /*
                Converts a square name (e.g., "e4") to a 6-bit integer representation.
                The conversion is based on the position of the square on the chessboard:
                - 'a1' corresponds to 0
                - 'h8' corresponds to 63

                Returns -1 for no en passant available from fen reading
            */
            if (squareName == "-")
            {
                return -1; // No en passant square
            }
            if (squareName.Length != 2)
            {
                throw new ArgumentException("Invalid square name format.");
            }
            int fileIndex = squareName[0] - 'a'; // 'a' corresponds to 0, 'b' to 1, ..., 'h' to 7
            int rankIndex = squareName[1] - '1'; // '1' corresponds to 0, '2' to 1, ..., '8' to 7

            if (fileIndex < 0 || fileIndex > 7 || rankIndex < 0 || rankIndex > 7)
            {
                throw new ArgumentException("Square name must be in the format 'a1' to 'h8'.");
            }

            return (rankIndex * 8) + fileIndex;
        }

        public static string SquareConversion(int squareData)
        {
            /*
                Converts a 6-bit integer representation of a square back to its string name (e.g., 0 to "a1", 63 to "h8").
                The conversion is based on the position of the square on the chessboard.
            */
            int fileIndex = squareData & 0x07; // Get the file index (0-7)
            int rankIndex = (squareData >> 3) & 0x07; // Get the rank index (0-7)

            char file = (char)('a' + fileIndex);
            char rank = (char)('1' + rankIndex);

            return $"{file}{rank}";
        }

        public static char PieceConversion(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => 'P',
                PieceType.Knight => 'N',
                PieceType.Bishop => 'B',
                PieceType.Rook => 'R',
                PieceType.Queen => 'Q',
                PieceType.King => 'K',
                PieceType.None => ' ',
                _ => throw new ArgumentOutOfRangeException(nameof(pieceType), $"Invalid piece type {pieceType}")
            };
        }

        public static PieceName PieceConversion(char piece)
        {
            return piece switch
            {
                'P' => PieceName.WhitePawn,
                'N' => PieceName.WhiteKnight,
                'B' => PieceName.WhiteBishop,
                'R' => PieceName.WhiteRook,
                'Q' => PieceName.WhiteQueen,
                'K' => PieceName.WhiteKing,
                'p' => PieceName.BlackPawn,
                'n' => PieceName.BlackKnight,
                'b' => PieceName.BlackBishop,
                'r' => PieceName.BlackRook,
                'q' => PieceName.BlackQueen,
                'k' => PieceName.BlackKing,
                _ => throw new ArgumentOutOfRangeException(nameof(piece), $"Invalid piece name {piece}")
            };
        }

        public static string BoardDiagram(Board board, bool blackAtTop = true, bool includeFen = true, bool includeZobristKey = true)
        {
            System.Text.StringBuilder result = new();

            for (int y = 0; y < 8; y++)
            {
                int rankIndex = blackAtTop ? 7 - y : y;
                result.AppendLine("+---+---+---+---+---+---+---+---+");

                for (int x = 0; x < 8; x++)
                {
                    int fileIndex = blackAtTop ? x : 7 - x;
                    int square = Operations.SquareIndex(rankIndex, fileIndex);
                    char piece = board.squares[square];
                    if (piece == '\0') piece = ' ';
                    result.Append($"| {piece} ");

                    if (x == 7)
                    {
                        // Show rank number
                        result.AppendLine($"| {rankIndex + 1}");
                    }
                }

                if (y == 7)
                {
                    // Show file names
                    result.AppendLine("+---+---+---+---+---+---+---+---+");
                    const string fileNames = "  a   b   c   d   e   f   g   h  ";
                    const string fileNamesRev = "  h   g   f   e   d   c   b   a  ";
                    result.AppendLine(blackAtTop ? fileNames : fileNamesRev);
                    result.AppendLine();

                    if (includeFen)
                    {
                        result.AppendLine($"Fen         : {ToFen(board)}");
                    }
                    if (includeZobristKey)
                    {
                        result.AppendLine($"Zobrist Key : {board.zobristHash:X16}");
                    }
                }
            }

            return result.ToString();
        }

        public static string MoveToUci(Move move)
        {
            string fromSquare = SquareConversion(move.From());
            string toSquare = SquareConversion(move.To());
            char promotedPiece = char.ToLower(PieceConversion(move.PromotionPieceType()));

            return $"{fromSquare}{toSquare}{promotedPiece}";
        }

        public static string ToFen(Board board)
        {
            string fen = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int emptyCount = 0;
                for (int file = 0; file < 8; file++)
                {
                    int squareIndex = Operations.SquareIndex(rank, file);
                    char piece = board.squares[squareIndex];
                    if (piece == '\0')
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen += emptyCount.ToString();
                            emptyCount = 0;
                        }
                        fen += piece;
                    }
                }
                if (emptyCount > 0)
                    fen += emptyCount.ToString();
                
                if (rank > 0)
                    fen += '/';
                
            }

            fen += board.sideToMove == SideColor.White ? " w " : " b ";

            string castlingRights = "";
            if (board.castlingRights[(int)CastlingRightsIndex.WhiteKingSide]) castlingRights += "K";
            if (board.castlingRights[(int)CastlingRightsIndex.WhiteQueenSide]) castlingRights += "Q";
            if (board.castlingRights[(int)CastlingRightsIndex.BlackKingSide]) castlingRights += "k";
            if (board.castlingRights[(int)CastlingRightsIndex.BlackQueenSide]) castlingRights += "q";
            fen += string.IsNullOrEmpty(castlingRights) ? "- " : castlingRights + " ";

            fen += board.enPassantSquare == -1 ? "- " : $"{SquareConversion(board.enPassantSquare)} ";
            fen += $"{board.halfmoveClock} {board.fullmoveNumber}";

            return fen;
        }
    }
}