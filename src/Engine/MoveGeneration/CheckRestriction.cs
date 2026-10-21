using Core.DataStructures;
using Core.Helpers;

namespace Engine.MoveGeneration;

public static class CheckRestriction
{
    public static ulong GetCheckingPieces(Board board)
    {
        /*
            Return a bitboard with a set bit for each checking piece
            An empty bitboard means the king is not in check
            A single bit set means the king is in a single check and must move, block or capture said piece
            Two bits set means the king is in double check and must move out of check, no other option
            There can't be more than two bits set: a triple check is impossible
        */
        ulong coverage = AttackCoverage.GetFullCoverage(board);
        ulong kingBitboard = board.GetBitboard(PieceType.King, board.sideToMove);
        int kingSquare = Operations.LsbIndex(kingBitboard);
        SideColor enemyColor = 1 - board.sideToMove;

        if ((kingBitboard & coverage) == 0) 
            return 0;

        ulong checkingPieces = 0;
        // Find the checking piece
        // Simulate an attack from the king to find the checking piece (all captures are symmetrical)

        int[] shifts = board.sideToMove == SideColor.White ? [7, 9] : [-9, -7];
        ulong pawnBitboard = board.GetBitboard(PieceType.Pawn, enemyColor);

        ulong leftPawn = Operations.ShiftByInt(kingBitboard & ~Bitboards.FILE_A, shifts[0]);
        ulong rightPawn = Operations.ShiftByInt(kingBitboard & ~Bitboards.FILE_H, shifts[1]);
        if ((leftPawn & pawnBitboard) != 0)
            checkingPieces |= leftPawn;
        if ((rightPawn & pawnBitboard) != 0)
            checkingPieces |= rightPawn;

        // Knight
        ulong knightBitboard = board.GetBitboard(PieceType.Knight, enemyColor);
        ulong possibleKnightSquares = Magics.Instance().GetKnightMoves(kingSquare);

        if ((possibleKnightSquares & knightBitboard) != 0)
            checkingPieces |= possibleKnightSquares & knightBitboard;

        // Sliders - Use of magic bitboards from the king square, ANDed with the opponents' respective sliders
        ulong occupied = board.GetFullBitboard();
        SideColor enemy = 1 - board.sideToMove;

        ulong diagonalAttacks = Magics.Instance().GetBishopMoves(kingSquare, occupied);
        ulong straightAttacks = Magics.Instance().GetRookMoves(kingSquare, occupied);

        ulong bishopsAndQueens =
            board.GetBitboard(PieceType.Bishop, enemy) |
            board.GetBitboard(PieceType.Queen, enemy);

        ulong rooksAndQueens =
            board.GetBitboard(PieceType.Rook, enemy) |
            board.GetBitboard(PieceType.Queen, enemy);

        checkingPieces |= diagonalAttacks & bishopsAndQueens;
        checkingPieces |= straightAttacks & rooksAndQueens;

        return checkingPieces;
    }

    public static List<ulong> GetPinnedPieceMasks(Board board)
    {
        /*
            Returns a list of mask bitboards
            The masks start on the pinning piece's square (included) and end on the king's square (excluded)
            Along this mask is a single pinned piece, which has it's movement restricted to just the set bits in the mask
            It's not possible for a piece to be pinned along two different lines, so considering a single mask is enough

            The logic for this method currently relies on multiple loops.
            Future improvements include:
                - making the logic faster (bitwise calculations)
                - adapting the logic to pieces other than the king
        */
        List<ulong> pinMasks = [];
        ulong kingBitboard = board.GetBitboard(PieceType.King, board.sideToMove);
        if (kingBitboard == 0)
            return pinMasks;

        int kingSquare = Operations.LsbIndex(kingBitboard);
        int kingRank = kingSquare / 8;
        int kingFile = kingSquare % 8;
        SideColor friendlyColor = board.sideToMove;
        SideColor enemyColor = 1 - friendlyColor;
        ulong friendlyPieces = board.GetSideBitboard(friendlyColor);
        ulong occupied = board.GetFullBitboard();
        ulong bishopsAndQueens =
            board.GetBitboard(PieceType.Bishop, enemyColor) |
            board.GetBitboard(PieceType.Queen, enemyColor);
        ulong rooksAndQueens =
            board.GetBitboard(PieceType.Rook, enemyColor) |
            board.GetBitboard(PieceType.Queen, enemyColor);
        int[,] directions =
        {
            { -1, -1 }, { -1, 0 }, { -1, 1 },
            {  0, -1 },            {  0, 1 },
            {  1, -1 }, {  1, 0 }, {  1, 1 }
        };

        for (int direction = 0; direction < directions.GetLength(0); direction++)
        {
            int rankStep = directions[direction, 0];
            int fileStep = directions[direction, 1];
            int rank = kingRank + rankStep;
            int file = kingFile + fileStep;
            ulong pinnedPiece = 0;
            ulong pinMask = 0;

            while (rank >= 0 && rank < 8 && file >= 0 && file < 8)
            {
                ulong square = 1UL << (rank * 8 + file);
                pinMask |= square;

                if ((occupied & square) != 0)
                {
                    if (pinnedPiece == 0)
                    {
                        if ((friendlyPieces & square) == 0)
                            break;

                        pinnedPiece = square;
                    }
                    else
                    {
                        bool diagonal = rankStep != 0 && fileStep != 0;
                        ulong compatibleSliders = diagonal ? bishopsAndQueens : rooksAndQueens;
                        if ((compatibleSliders & square) != 0)
                            pinMasks.Add(pinMask);

                        break;
                    }
                }

                rank += rankStep;
                file += fileStep;
            }
        }

        return pinMasks;
    }

}