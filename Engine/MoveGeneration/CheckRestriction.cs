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

        Magics magics = Magics.Instance();
        BitBoard kingRays = magics.GetRay(kingSquare);

        if (sliders | kingRays == BitBoard.Empty)
            return BitBoard.Empty;



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

    private static BitBoard GetPossiblePinRays(BitBoard sliders, int kingSquare)
    {
        BitBoard pinRays = BitBoard.Empty;

        foreach (int sliderIndex in sliders)
        {
            // Calculate step direction
            int kingRank = kingSquare / 8, kingFile = kingSquare % 8;
            int sliderRank = sliderIndex / 8, sliderFile = sliderIndex % 8;
            int deltaRank = Math.Sign(sliderRank - kingRank);
            int deltaFile = Math.Sign(sliderFile - kingFile);

            // Step from king towards slider
            int rank = kingRank + deltaRank, file = kingFile + deltaFile;
            BitBoard ray = BitBoard.Empty;
            while (rank != sliderRank || file != sliderFile)
            {
                ray |= BitBoard.FromSquare(rank * 8 + file);
                rank += deltaRank;
                file += deltaFile;
            }
            pinRays |= ray;
        }
        return pinRays;
    }

}