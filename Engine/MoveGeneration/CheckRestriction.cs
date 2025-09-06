using Core.Board;
using Core.Helpers;

namespace Engine.MoveGeneration;

public static class CheckRestriction
{
    public static List<BitBoard> GetPinRays(BoardState board, int kingSquare)
    {
        BitBoard kingBitboard = BitBoard.FromSquare(kingSquare);
        SideColor player = board.GetPieceAtSquare(kingSquare) == 'K' ? SideColor.White : SideColor.Black;
        SideColor opponent = player == SideColor.White ? SideColor.Black : SideColor.White;
        BitBoard diagonalSliders = GetEnemySliders(board, opponent, true);
        BitBoard straightSliders = GetEnemySliders(board, opponent, false);

        Magics magics = Magics.Instance();
        BitBoard kingRays = magics.GetRays(kingSquare);

        diagonalSliders &= kingRays;
        if (diagonalSliders == 0)
            return [];

        List<BitBoard> pinRays = GetPossiblePinRays(diagonalSliders, kingSquare, true);

        straightSliders &= kingRays;
        if (straightSliders == 0)
            return pinRays;

        pinRays.AddRange(GetPossiblePinRays(straightSliders, kingSquare, false));

        return pinRays;
    }

    private static BitBoard GetEnemySliders(BoardState board, SideColor enemyColor, bool isDiagonal)
    {
        if (enemyColor == SideColor.White)
        {
            return board.WhiteQueens | (isDiagonal ? board.WhiteBishops : board.WhiteRooks);
        }
        else
        {
            return board.BlackQueens | (isDiagonal ? board.BlackBishops : board.BlackRooks);
        }
    }

    private static List<BitBoard> GetPossiblePinRays(BitBoard sliders, int kingSquare, bool isDiagonal)
    {
        List<BitBoard> pinRays = [];

        while (sliders != 0)
        {
            int sliderIndex = BitBoard.PopLSB(ref sliders);

            int kingRank = kingSquare / 8, kingFile = kingSquare % 8;
            int sliderRank = sliderIndex / 8, sliderFile = sliderIndex % 8;
            int deltaRank = Math.Sign(sliderRank - kingRank);
            int deltaFile = Math.Sign(sliderFile - kingFile);

            // Only generate rays if direction matches isDiagonal
            bool isSliderDiagonal = Math.Abs(deltaRank) == 1 && Math.Abs(deltaFile) == 1;
            bool isSliderStraight = (deltaRank == 0 && deltaFile != 0) || (deltaFile == 0 && deltaRank != 0);

            if ((isDiagonal && !isSliderDiagonal) || (!isDiagonal && !isSliderStraight))
            continue;

            int rank = kingRank + deltaRank, file = kingFile + deltaFile;
            BitBoard ray = BitBoard.Empty;
            while (rank != sliderRank || file != sliderFile)
            {
            ray |= BitBoard.FromSquare(rank * 8 + file);
            rank += deltaRank;
            file += deltaFile;
            }
            pinRays.Add(ray);
        }
        return pinRays;
    }

    public static BitBoard GetRestrictedPieces(BitBoard pieces, List<BitBoard> pinRays)
    {
        BitBoard restrictedPieces = BitBoard.Empty;
        foreach (var ray in pinRays)
        {
            restrictedPieces |= ray & pieces;
        }
        return restrictedPieces;
    }

    public static BitBoard GetDiagonallyStuck(BitBoard pieces, List<BitBoard> pinRays)
    {
        BitBoard stuckPieces = BitBoard.Empty;
        foreach (var ray in pinRays)
        {
            int firstSquare = ray.LsbIndex();
            if (firstSquare == -1) continue;

            if ((BitBoard.RankBitBoard(firstSquare) & ray).PopCount() > 1 ||
                (BitBoard.FileBitBoard(firstSquare) & ray).PopCount() > 1)
            {
                continue; // Not a diagonal ray
            }

            BitBoard piecesOnRay = ray & pieces;
            if (piecesOnRay.PopCount() == 1)
            {
                stuckPieces |= piecesOnRay;
            }
        }
        return stuckPieces;
    }

    public static BitBoard GetVerticallyStuck(BitBoard pieces, List<BitBoard> pinRays)
    {
        BitBoard stuckPieces = BitBoard.Empty;
        foreach (var ray in pinRays)
        {
            int firstSquare = ray.LsbIndex();
            if (firstSquare == -1) continue;

            // Check if the ray is vertical (file)
            if ((BitBoard.FileBitBoard(firstSquare) & ray).PopCount() <= 1)
                continue; // Not a vertical ray

            BitBoard piecesOnRay = ray & pieces;
            if (piecesOnRay.PopCount() == 1)
            {
                stuckPieces |= piecesOnRay;
            }
        }
        return stuckPieces;
    }

    public static BitBoard GetHorizontallyStuck(BitBoard pieces, List<BitBoard> pinRays)
    {
        BitBoard stuckPieces = BitBoard.Empty;
        foreach (var ray in pinRays)
        {
            int firstSquare = ray.LsbIndex();
            if (firstSquare == -1) continue;

            // Check if the ray is horizontal (rank)
            if ((BitBoard.RankBitBoard(firstSquare) & ray).PopCount() <= 1)
                continue; // Not a horizontal ray

            BitBoard piecesOnRay = ray & pieces;
            if (piecesOnRay.PopCount() == 1)
            {
                stuckPieces |= piecesOnRay;
            }
        }
        return stuckPieces;
    }
    
}