using Core.Board;
using Engine.MoveGeneration;

namespace Core.Helpers;

public static class AttackCoverage
{
    public static BitBoard GetAttackCoverage(BoardState board, SideColor color, out BitBoard checkingPieces, bool includeOccupiedSquares = true, bool considerPins = false)
    {
        Magics magics = Magics.Instance();
        int startSquare;
        BitBoard attackCoverage = 0ul;
        BitBoard targetSquares = includeOccupiedSquares ? BitBoard.Full : ~board.AllPieces;
        checkingPieces = 0ul;
        BitBoard newSquares;
        
        BitBoard queensBitBoard = board.GetBitboard(color, PieceType.Queen);
        BitBoard rooksBitBoard = board.GetBitboard(color, PieceType.Rook);
        BitBoard bishopsBitBoard = board.GetBitboard(color, PieceType.Bishop);
        BitBoard knightsBitBoard = board.GetBitboard(color, PieceType.Knight);
        BitBoard pawnsBitBoard = board.GetBitboard(color, PieceType.Pawn);
        BitBoard kingBitBoard = board.GetBitboard(color, PieceType.King);
        BitBoard opponentKingBitboard = board.GetBitboard(color == SideColor.White ? SideColor.Black : SideColor.White, PieceType.King);

        // Queens
        while ((startSquare = BitBoard.PopLSB(ref queensBitBoard)) != -1)
        {
            newSquares = magics.GetQueenAttacks(startSquare, board.AllPieces.ToU64());
            if ((newSquares & opponentKingBitboard) != 0)
                checkingPieces.SetBit(startSquare);
            attackCoverage |= newSquares & targetSquares;
        }

        // Rooks
        while ((startSquare = BitBoard.PopLSB(ref rooksBitBoard)) != -1)
        {
            newSquares = magics.GetRookAttacks(startSquare, board.AllPieces.ToU64());
            if ((newSquares & opponentKingBitboard) != 0)
                checkingPieces.SetBit(startSquare);
            attackCoverage |= newSquares & targetSquares;
        }

        // Bishops
        while ((startSquare = BitBoard.PopLSB(ref bishopsBitBoard)) != -1)
        {
            newSquares = magics.GetBishopAttacks(startSquare, board.AllPieces.ToU64());
            if ((newSquares & opponentKingBitboard) != 0)
                checkingPieces.SetBit(startSquare);
            attackCoverage |= newSquares & targetSquares;
        }

        // Knights
        while ((startSquare = BitBoard.PopLSB(ref knightsBitBoard)) != -1)
        {
            newSquares = magics.GetKnightMoves(startSquare);
            if ((newSquares & opponentKingBitboard) != 0)
                checkingPieces.SetBit(startSquare);
            attackCoverage |= newSquares & targetSquares;
        }

        // Pawns
        newSquares = GetPawnAttackCoverage(pawnsBitBoard, color, BitBoard.Empty);
        attackCoverage |= newSquares & targetSquares;

        int[] pawnCheckOffsets = color == SideColor.White ? [-7, -9] : [7, 9];
        BitBoard checkingPawns = opponentKingBitboard.Shift(pawnCheckOffsets[0]) | opponentKingBitboard.Shift(pawnCheckOffsets[1]);
        checkingPieces |= (pawnsBitBoard & checkingPawns);

        // King (missing checking piece logic as king cannot directly check the opponent)
        newSquares = magics.GetKingMoves(startSquare = BitBoard.PopLSB(ref kingBitBoard));
        attackCoverage |= newSquares & targetSquares;
        

        return attackCoverage;
    }

    public static BitBoard GetPawnAttackCoverage(BitBoard bitboard, SideColor color, BitBoard excludedSquares)
    {
        int[] captureOffsets = color == SideColor.White ? [7, 9] : [-9, -7];
        return (bitboard.Shift(captureOffsets[0]) | bitboard.Shift(captureOffsets[1])) & ~excludedSquares;
    }
}