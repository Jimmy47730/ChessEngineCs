using Core.DataStructures;
using Core.Helpers;

namespace Engine.MoveGeneration;

public static class AttackCoverage
{
    /*
        These methods calculate the coverage of all pieces, including the already occupied squares.
        Does not exclude squares "ghost attacked", or attacked by a piece pinned on another line.
        That feature, still to be coded, belongs to the evaluation module.
    */
    public static ulong PawnCoverage(Board board, bool opponent = true)
    {
        SideColor color = opponent ? 1 - board.sideToMove : board.sideToMove;
        ulong bitboard = board.GetBitboard(PieceType.Pawn, color);
        ulong coverage = 0;

        // The shifts are saved based on color, with index 0 being towards the 'a' file
        int[] shifts = color == SideColor.White ? [7, 9] : [-9, 7];
        
        coverage |= Operations.ShiftByInt(bitboard & ~Bitboards.FILE_A, shifts[0]);
        coverage |= Operations.ShiftByInt(bitboard & ~Bitboards.FILE_H, shifts[1]);

        return coverage;
    }
    public static ulong KnightCoverage(Board board, bool opponent = true)
    {
        SideColor color = opponent ? 1 - board.sideToMove : board.sideToMove;
        ulong bitboard = board.GetBitboard(PieceType.Knight, color);
        int startSquare;
        ulong knightMoves = 0;

        while ((startSquare = Operations.PopLSB(ref bitboard)) != -1)
        {
            knightMoves |= Magics.Instance().GetKnightMoves(startSquare);
        }

        return knightMoves;
    }
    public static ulong BishopCoverage(Board board, bool opponent = true)
    {
        SideColor color = opponent ? 1 - board.sideToMove : board.sideToMove;
        ulong bitboard = board.GetBitboard(PieceType.Bishop, color);
        ulong occupied = board.GetFullBitboard();
        int startSquare;
        ulong bishopMoves = 0;

        while ((startSquare = Operations.PopLSB(ref bitboard)) != -1)
        {
            bishopMoves |= Magics.Instance().GetBishopMoves(startSquare, occupied);
        }

        return bishopMoves;
    }
    public static ulong RookCoverage(Board board, bool opponent = true)
    {
        SideColor color = opponent ? 1 - board.sideToMove : board.sideToMove;
        ulong bitboard = board.GetBitboard(PieceType.Rook, color);
        ulong occupied = board.GetFullBitboard();
        int startSquare;
        ulong rookMoves = 0;

        while ((startSquare = Operations.PopLSB(ref bitboard)) != -1)
        {
            rookMoves |= Magics.Instance().GetRookMoves(startSquare, occupied);
        }

        return rookMoves;
    }
    public static ulong QueenCoverage(Board board, bool opponent = true)
    {
        SideColor color = opponent ? 1 - board.sideToMove : board.sideToMove;
        ulong bitboard = board.GetBitboard(PieceType.Queen, color);
        ulong occupied = board.GetFullBitboard();
        int startSquare;
        ulong queenMoves = 0;

        while ((startSquare = Operations.PopLSB(ref bitboard)) != -1)
        {
            queenMoves |= Magics.Instance().GetQueenMoves(startSquare, occupied);
        }

        return queenMoves;
    }
    public static ulong KingCoverage(Board board, bool opponent = true)
    {
        SideColor color = opponent ? 1 - board.sideToMove : board.sideToMove;
        ulong bitboard = board.GetBitboard(PieceType.King, color);
        int startSquare;
        ulong kingMoves = 0;

        while ((startSquare = Operations.PopLSB(ref bitboard)) != -1)
        {
            kingMoves |= Magics.Instance().GetKingMoves(startSquare);
        }

        return kingMoves;
    }

    // This method returns the bitboard of all squares controlled by one side, aka where the opposite king can't move
    public static ulong GetFullCoverage(Board board, bool opponent = true)
    {
        return PawnCoverage(board, opponent)
             | KnightCoverage(board, opponent)
             | BishopCoverage(board, opponent)
             | RookCoverage(board, opponent)
             | QueenCoverage(board, opponent)
             | KingCoverage(board, opponent);
    }
}