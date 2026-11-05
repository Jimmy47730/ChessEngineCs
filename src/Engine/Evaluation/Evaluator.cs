using Core.DataStructures;
using Core.Helpers;

namespace Engine.Evaluation;

public static class Evaluator
{
    private const int pawnValue = 100;
    private const int knightValue = 300;
    private const int bishopValue = 300;
    private const int rookValue = 500;
    private const int queenValue = 900;

    public static int EvaluatePosition(Board board)
    {
        // Temporary material-only evaluation
        int score = 0; 

        score += Operations.PopCount(board.GetBitboard(PieceName.WhitePawn)) * pawnValue;
        score += Operations.PopCount(board.GetBitboard(PieceName.WhiteKnight)) * knightValue;
        score += Operations.PopCount(board.GetBitboard(PieceName.WhiteBishop)) * bishopValue;
        score += Operations.PopCount(board.GetBitboard(PieceName.WhiteRook)) * rookValue;
        score += Operations.PopCount(board.GetBitboard(PieceName.WhiteQueen)) * queenValue;

        score -= Operations.PopCount(board.GetBitboard(PieceName.BlackPawn)) * pawnValue;
        score -= Operations.PopCount(board.GetBitboard(PieceName.BlackKnight)) * knightValue;
        score -= Operations.PopCount(board.GetBitboard(PieceName.BlackBishop)) * bishopValue;
        score -= Operations.PopCount(board.GetBitboard(PieceName.BlackRook)) * rookValue;
        score -= Operations.PopCount(board.GetBitboard(PieceName.BlackQueen)) * queenValue;

        return board.sideToMove == SideColor.White ? score : -score;
    }
}