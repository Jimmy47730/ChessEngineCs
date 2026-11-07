using Core.DataStructures;
using Core.Helpers;

namespace Engine.Evaluation;

public static class Evaluator
{
    private const int pawnValue = 100;
    private const int knightValue = 310;
    private const int bishopValue = 325; // Might bump it up/down later
    private const int rookValue = 500;
    private const int queenValue = 920;

    public static int EvaluatePosition(Board board)
    {
        int score = 0; 

        score += CountMaterial(board);
        score += EvaluateMaterialImbalance(board);

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
                + bishops on low pawns
                - knights on low pawns
                + rook with less than 5 pawns
            TODO:
                + piece for 3 pawns
                + rook for piece and pawn
                + 2 pieces for rook in middlegame
                - 2 pieces for rook in endgame
                + queen for rook, piece and pawn
                + queen for 2 rooks in middlegame
                - queen for three pieces
        */
        
        int imbalance = 0;
        int whitePawnCount = Operations.PopCount(board.GetBitboard(PieceName.WhitePawn));
        int blackPawnCount = Operations.PopCount(board.GetBitboard(PieceName.BlackPawn));
        int totalPawnCount = whitePawnCount + blackPawnCount;

        // White bishop pair bonus
        bool whiteBishopPair = false;
        ulong bishopBitboard = board.GetBitboard(PieceName.WhiteBishop);
        int firstBishop = Operations.PopLSB(ref bishopBitboard);
        int secondBishop = -1;
        while (firstBishop != -1 || (secondBishop = Operations.PopLSB(ref bishopBitboard)) != -1)
        {
            if ((firstBishop / 8 + firstBishop % 8) % 2 != (secondBishop / 8 + secondBishop % 8) % 2) // Check if bishops are on opposite colors
            {
                whiteBishopPair = true;
                break;
            }
        }

        // Black bishop pair bonus
        bool blackBishopPair = false;
        bishopBitboard = board.GetBitboard(PieceName.BlackBishop);
        firstBishop = Operations.PopLSB(ref bishopBitboard);
        secondBishop = -1;
        while (firstBishop != -1 || (secondBishop = Operations.PopLSB(ref bishopBitboard)) != -1)
        {
            if ((firstBishop / 8 + firstBishop % 8) % 2 != (secondBishop / 8 + secondBishop % 8) % 2) // Check if bishops are on opposite colors
            {
                blackBishopPair = true;
                break;
            }
        }

        // This formula applies the bonus for the bishop pair, ranging from 38 to 70 based on the inverse of the number of remaining pawns
        imbalance += whiteBishopPair ? 30 + (20 - totalPawnCount) * 2 : 0;
        imbalance -= blackBishopPair ? 30 + (20 - totalPawnCount) * 2 : 0;

        // Penalty for having no pawns
        imbalance += whitePawnCount == 0 ? -30 : 0;
        imbalance -= blackPawnCount == 0 ? -30 : 0;

        // Bouns for bishops on low pawns
        int whiteBishopCount = Operations.PopCount(board.GetBitboard(PieceName.WhiteBishop));
        int blackBishopCount = Operations.PopCount(board.GetBitboard(PieceName.BlackBishop));

        imbalance += whiteBishopCount * (totalPawnCount <= 10 ? 10 : 0);
        imbalance -= blackBishopCount * (totalPawnCount <= 10 ? 10 : 0);

        // Penalty for knights on low pawns
        int whiteKnightCount = Operations.PopCount(board.GetBitboard(PieceName.WhiteKnight));
        int blackKnightCount = Operations.PopCount(board.GetBitboard(PieceName.BlackKnight));

        imbalance += whiteKnightCount * (totalPawnCount <= 8 ? -10 : 0);
        imbalance -= blackKnightCount * (totalPawnCount <= 8 ? -10 : 0);

        // Bonus for rooks with less than 5 pawns
        int whiteRookCount = Operations.PopCount(board.GetBitboard(PieceName.WhiteRook));
        int blackRookCount = Operations.PopCount(board.GetBitboard(PieceName.BlackRook));

        imbalance += whiteRookCount * (whitePawnCount < 5 ? 15 : 0);
        imbalance -= blackRookCount * (blackPawnCount < 5 ? 15 : 0);

        return imbalance;
    }
}