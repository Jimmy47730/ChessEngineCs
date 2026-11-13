using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;

namespace Engine.Search;

public static class MoveOrdering
{
    private const int checkBonus = 1_000_000;
    private const int promotionBonus = 200_000;
    private const int captureBonus = 100_000;

    public static void OrderMoves(Board board, List<Move> moves)
    {
        Span<int> scores = stackalloc int[moves.Count];

        for (int i = 0; i < moves.Count; i++)
            scores[i] = GetScore(board, moves[i]);

        for (int i = 1; i < moves.Count; i++)
        {
            Move move = moves[i];
            int score = scores[i];
            int j = i - 1;

            while (j >= 0 && scores[j] < score)
            {
                moves[j + 1] = moves[j];
                scores[j + 1] = scores[j];
                j--;
            }

            moves[j + 1] = move;
            scores[j + 1] = score;
        }
    }

    private static int GetScore(Board board, Move move)
    {
        char movingPiece = board.GetPieceAt(move.From());
        PieceType movingPieceType = GetPieceType(movingPiece);
        int score = 0;

        if (move.IsCapture())
        {
            char capturedPiece = move.IsEnPassant()
                ? StringRepr.PieceConversion(PieceType.Pawn)
                : board.GetPieceAt(move.To());
            score += captureBonus +
                GetPieceValue(GetPieceType(capturedPiece)) * 10 -
                GetPieceValue(movingPieceType);
        }

        if (move.IsPromotion())
        {
            score += promotionBonus +
                GetPieceValue(move.PromotionPieceType()) -
                GetPieceValue(PieceType.Pawn);
        }

        UndoInfo undoInfo = board.ApplyMove(move);
        bool givesCheck;
        try
        {
            givesCheck = CheckRestriction.GetCheckingPieces(board) != 0;
        }
        finally
        {
            board.UndoMove(move, undoInfo);
        }

        if (givesCheck)
            score += checkBonus;

        return score;
    }

    private static PieceType GetPieceType(char piece)
    {
        if (piece == '\0')
            return PieceType.None;

        return (PieceType)((int)StringRepr.PieceConversion(piece) % 6);
    }

    private static int GetPieceValue(PieceType pieceType) => pieceType switch
    {
        PieceType.Pawn => 100,
        PieceType.Knight => 310,
        PieceType.Bishop => 325,
        PieceType.Rook => 500,
        PieceType.Queen => 920,
        _ => 0
    };
}
