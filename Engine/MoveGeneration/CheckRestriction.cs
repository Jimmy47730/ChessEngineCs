using Core.Board;
using Core.Helpers;

namespace Engine.MoveGeneration;

public static class CheckRestriction
{
    public static BitBoard GetPinRays(BoardState board, int kingSquare)
    {
        BitBoard pinRays = BitBoard.Empty;
        BitBoard kingBitboard = BitBoard.FromSquare(kingSquare);
        SideColor player = board.GetPieceAtSquare(kingSquare) == 'K' ? SideColor.White : SideColor.Black;
        SideColor opponent = player == SideColor.White ? SideColor.Black : SideColor.White;
        BitBoard sliders = GetEnemySliders(board, opponent);

        if ((sliders & kingBitboard) == BitBoard.Empty)
        {
            return BitBoard.Empty;
        }

        Magics magics = Magics.Instance();
        


        return pinRays;
    }

    private static BitBoard GetEnemySliders(BoardState board, SideColor enemyColor)
    {
        if (enemyColor == SideColor.White)
        {
            return board.WhiteBishops | board.WhiteRooks | board.WhiteQueens;
        }
        else
        {
            return board.BlackBishops | board.BlackRooks | board.BlackQueens;
        }
    }
}