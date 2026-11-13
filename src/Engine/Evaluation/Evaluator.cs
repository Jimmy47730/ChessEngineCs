using System.Numerics;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;

namespace Engine.Evaluation;

public static class Evaluator
{
    private const int pawnValue = 100;
    private const int knightValue = 310;
    private const int bishopValue = 325; // Might bump it up/down later
    private const int rookValue = 500;
    private const int queenValue = 920;
    private const int maximumMaterialPhase = 24;

    // Tables are indexed from a1 to h8, so the supplied a8-first rows are reversed.
    private static readonly int[] pawnMiddleGameTable =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        5, 10, 10, -20, -20, 10, 10, 5,
        5, -5, -10, 0, 0, -10, -5, 5,
        0, 0, 0, 20, 20, 0, 0, 0,
        5, 5, 10, 25, 25, 10, 5, 5,
        10, 10, 20, 30, 30, 20, 10, 10,
        50, 50, 50, 50, 50, 50, 50, 50,
        0, 0, 0, 0, 0, 0, 0, 0
    ];
    private static readonly int[] pawnEndGameTable =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        -10, -10, -10, -10, -10, -10, -10, -10,
        -5, -5, -5, -5, -5, -5, -5, -5,
        0, 0, 0, 0, 0, 0, 0, 0,
        10, 10, 10, 10, 10, 10, 10, 10,
        20, 20, 20, 20, 20, 20, 20, 20,
        50, 50, 50, 50, 50, 50, 50, 50,
        0, 0, 0, 0, 0, 0, 0, 0
    ];
    private static readonly int[] knightTable =
    [
        -50, -40, -30, -30, -30, -30, -40, -50,
        -40, -20, 0, 5, 5, 0, -20, -40,
        -30, 5, 10, 15, 15, 10, 5, -30,
        -30, 0, 15, 20, 20, 15, 0, -30,
        -30, 5, 15, 20, 20, 15, 5, -30,
        -30, 0, 10, 15, 15, 10, 0, -30,
        -40, -20, 0, 0, 0, 0, -20, -40,
        -50, -40, -30, -30, -30, -30, -40, -50
    ];
    private static readonly int[] bishopTable =
    [
        -20, -10, -10, -10, -10, -10, -10, -20,
        -10, 5, 0, 0, 0, 0, 5, -10,
        -10, 10, 10, 10, 10, 10, 10, -10,
        -10, 0, 10, 10, 10, 10, 0, -10,
        -10, 5, 5, 10, 10, 5, 5, -10,
        -10, 0, 5, 10, 10, 5, 0, -10,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -20, -10, -10, -10, -10, -10, -10, -20
    ];
    private static readonly int[] rookTable =
    [
        0, 0, 0, 5, 5, 0, 0, 0,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        5, 10, 10, 10, 10, 10, 10, 5,
        0, 0, 0, 0, 0, 0, 0, 0
    ];
    private static readonly int[] queenTable =
    [
        -20, -10, -10, -5, -5, -10, -10, -20,
        -10, 0, 5, 0, 0, 0, 0, -10,
        -10, 5, 5, 5, 5, 5, 0, -10,
        0, 0, 5, 5, 5, 5, 0, -5,
        -5, 0, 5, 5, 5, 5, 0, -5,
        -10, 0, 5, 5, 5, 5, 0, -10,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -20, -10, -10, -5, -5, -10, -10, -20
    ];
    private static readonly int[] kingMiddleGameTable =
    [
        20, 30, 10, 0, 0, 10, 30, 20,
        20, 20, 0, 0, 0, 0, 20, 20,
        -10, -20, -20, -20, -20, -20, -20, -10,
        -20, -30, -30, -40, -40, -30, -30, -20,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30
    ];
    private static readonly int[] kingEndGameTable =
    [
        -50, -30, -30, -30, -30, -30, -30, -50,
        -30, -30, 0, 0, 0, 0, -30, -30,
        -30, -10, 20, 30, 30, 20, -10, -30,
        -30, -10, 30, 40, 40, 30, -10, -30,
        -30, -10, 30, 40, 40, 30, -10, -30,
        -30, -10, 20, 30, 30, 20, -10, -30,
        -30, -20, -10, 0, 0, -10, -20, -30,
        -50, -40, -30, -20, -20, -30, -40, -50
    ];

    public static int EvaluatePosition(Board board)
    {
        int score = 0; 

        score += EvaluateMaterialAndPieceSquareTables(board);
        score += EvaluateMaterialImbalance(board);

        return board.sideToMove == SideColor.White ? score : -score;
    }

    private static int EvaluateMaterialAndPieceSquareTables(Board board)
    {
        int materialPhase = GetMaterialPhase(board);
        int score = 0;

        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            SideColor side = (SideColor)sideIndex;
            int sideSign = side == SideColor.White ? 1 : -1;

            for (int pieceIndex = (int)PieceType.Pawn; pieceIndex <= (int)PieceType.King; pieceIndex++)
            {
                PieceType piece = (PieceType)pieceIndex;
                ulong pieces = board.GetBitboard(piece, side);
                int square;

                while ((square = Operations.PopLSB(ref pieces)) != -1)
                {
                    int tableSquare = side == SideColor.White ? square : square ^ 56;
                    score += sideSign * (GetPieceValue(piece) +
                        GetPieceSquareValue(piece, tableSquare, materialPhase));
                }
            }
        }

        return score;
    }

    private static int GetMaterialPhase(Board board)
    {
        int phase = 0;
        phase += BitOperations.PopCount(board.GetBitboard(PieceType.Knight, SideColor.White));
        phase += BitOperations.PopCount(board.GetBitboard(PieceType.Knight, SideColor.Black));
        phase += BitOperations.PopCount(board.GetBitboard(PieceType.Bishop, SideColor.White));
        phase += BitOperations.PopCount(board.GetBitboard(PieceType.Bishop, SideColor.Black));
        phase += 2 * BitOperations.PopCount(board.GetBitboard(PieceType.Rook, SideColor.White));
        phase += 2 * BitOperations.PopCount(board.GetBitboard(PieceType.Rook, SideColor.Black));
        phase += 4 * BitOperations.PopCount(board.GetBitboard(PieceType.Queen, SideColor.White));
        phase += 4 * BitOperations.PopCount(board.GetBitboard(PieceType.Queen, SideColor.Black));

        return Math.Min(phase, maximumMaterialPhase);
    }

    private static int GetPieceValue(PieceType piece) => piece switch
    {
        PieceType.Pawn => pawnValue,
        PieceType.Knight => knightValue,
        PieceType.Bishop => bishopValue,
        PieceType.Rook => rookValue,
        PieceType.Queen => queenValue,
        _ => 0
    };

    private static int GetPieceSquareValue(PieceType piece, int square, int materialPhase)
    {
        return piece switch
        {
            PieceType.Pawn => Interpolate(pawnMiddleGameTable[square], pawnEndGameTable[square], materialPhase),
            PieceType.Knight => knightTable[square],
            PieceType.Bishop => bishopTable[square],
            PieceType.Rook => rookTable[square],
            PieceType.Queen => queenTable[square],
            PieceType.King => Interpolate(kingMiddleGameTable[square], kingEndGameTable[square], materialPhase),
            _ => 0
        };
    }

    private static int Interpolate(int middleGameValue, int endGameValue, int materialPhase)
    {
        return (middleGameValue * materialPhase +
            endGameValue * (maximumMaterialPhase - materialPhase)) / maximumMaterialPhase;
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

    // Force the opponent king to the edge of the board in the endgame, usually leading to some sort of mate.
    private static int ForceOpponentKingToEdge(Board board)
    {
        int score = 0;

        if (GetMaterialPhase(board) > 4)
            return score;

        int whiteKingSquare = Operations.LsbIndex(board.GetBitboard(PieceName.WhiteKing));
        int blackKingSquare = Operations.LsbIndex(board.GetBitboard(PieceName.BlackKing));

        // If the opponent king is on the edge of the board, give a bonus
        score += (int)((3.5 - Math.Abs(blackKingSquare / 8)) * 20);
        score += (int)((3.5 - Math.Abs(blackKingSquare % 8)) * 20);
        score -= (int)((3.5 - Math.Abs(whiteKingSquare / 8)) * 20);
        score -= (int)((3.5 - Math.Abs(whiteKingSquare % 8)) * 20);

        // Give a bonus for the opponent king having less legal moves
        SideColor originalSideColor = board.ChangeSideToMove(SideColor.Black);
        score += (8 - MoveGenerator.GenerateKingMoves(board, false).Count) * 8;
        board.ChangeSideToMove(SideColor.White);
        score -= (8 - MoveGenerator.GenerateKingMoves(board, false).Count) * 8;
        board.ChangeSideToMove(originalSideColor);

        return score;
    }
}