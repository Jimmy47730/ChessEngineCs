using Core.Board;

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
            */
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

        public static string PieceConversion(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => "",
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.Rook => "R",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => throw new ArgumentOutOfRangeException(nameof(pieceType), $"Invalid piece type {pieceType}")
            };
        }

        public static PieceType PieceConversion(string pieceName)
        {
            return pieceName switch
            {
                "" => PieceType.Pawn,
                "N" => PieceType.Knight,
                "B" => PieceType.Bishop,
                "R" => PieceType.Rook,
                "Q" => PieceType.Queen,
                "K" => PieceType.King,
                _ => throw new ArgumentOutOfRangeException(nameof(pieceName), $"Invalid piece name {pieceName}")
            };
        }

        public static char PromotionUciConversion(PromotionType promotionType)
        {
            return promotionType switch
            {
                PromotionType.Queen => 'q',
                PromotionType.Rook => 'r',
                PromotionType.Bishop => 'b',
                PromotionType.Knight => 'n',
                PromotionType.None => ' ',
                _ => throw new ArgumentOutOfRangeException(nameof(promotionType), $"Invalid promotion type {promotionType}")
            };
        }

        public static string PromotionConversion(PromotionType promotionType)
        {
            return promotionType switch
            {
                PromotionType.None => "",
                PromotionType.Queen => "=Q",
                PromotionType.Rook => "=R",
                PromotionType.Bishop => "=B",
                PromotionType.Knight => "=N",
                _ => throw new ArgumentOutOfRangeException(nameof(promotionType), $"Invalid promotion type {promotionType}")
            };
        }

        public static string BoardDiagram(BoardState board, bool blackAtTop = true, bool includeFen = true, bool includeZobristKey = true)
        {
            System.Text.StringBuilder result = new();

            for (int y = 0; y < 8; y++)
            {
                int rankIndex = blackAtTop ? 7 - y : y;
                result.AppendLine("+---+---+---+---+---+---+---+---+");

                for (int x = 0; x < 8; x++)
                {
                    int fileIndex = blackAtTop ? x : 7 - x;
                    int square = Square.Index(rankIndex, fileIndex);
                    char piece = board.GetPieceAtSquare(square);
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
                        result.AppendLine($"Fen         : {FenUtility.WriteFen(board)}");
                    }
                    if (includeZobristKey)
                    {
                        result.AppendLine($"Zobrist Key : {board.HashKey:X16}");
                    }
                }
            }

            return result.ToString();
        }

        public static string MoveUciConversion(Move move)
        {
            string fromSquare = SquareConversion(move.FromSquare);
            string toSquare = SquareConversion(move.ToSquare);
            char promotedPiece = PromotionUciConversion(move.PromotedPiece);

            return $"{fromSquare}{toSquare}{promotedPiece}";
        }

    }
}