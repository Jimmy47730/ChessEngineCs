using Core.DataStructures;
using Core.Helpers;

namespace Engine.Evaluation;

public static class Evaluator
{
    private const int pawnValue = 100;
    private const int knightValue = 300;
    private const int bishopValue = 320;
    private const int rookValue = 500;
    private const int queenValue = 900;

    public static int EvaluatePosition(Board board)
    {
        int score = 0; 

        score += CountMaterial(board);



        return board.sideToMove == SideColor.White ? score : -score;
    }

    private static int CountMaterial(Board board)
    {
        int material = 0;

        material += Operations.PopCount(board.GetBitboard(PieceName.WhitePawn)) * pawnValue;
        material += Operations.PopCount(board.GetBitboard(PieceName.WhiteKnight)) * knightValue;
        material += Operations.PopCount(board.GetBitboard(PieceName.WhiteBishop)) * bishopValue;
        material += Operations.PopCount(board.GetBitboard(PieceName.WhiteRook)) * rookValue;
        material += Operations.PopCount(board.GetBitboard(PieceName.WhiteQueen)) * queenValue;

        material -= Operations.PopCount(board.GetBitboard(PieceName.BlackPawn)) * pawnValue;
        material -= Operations.PopCount(board.GetBitboard(PieceName.BlackKnight)) * knightValue;
        material -= Operations.PopCount(board.GetBitboard(PieceName.BlackBishop)) * bishopValue;
        material -= Operations.PopCount(board.GetBitboard(PieceName.BlackRook)) * rookValue;
        material -= Operations.PopCount(board.GetBitboard(PieceName.BlackQueen)) * queenValue;

        return material;
    }

    private static int EvaluateMaterialImbalance(Board board)
    {
        /*
            Currently considered imbalances include:
                + bishop pair
                - no pawns
                + advantageous piece combos on same material
            TODO:
                - idk man
        */
        int imbalance = 0;



        return imbalance;
    }
}