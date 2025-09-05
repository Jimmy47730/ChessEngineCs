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
        BitBoard sliders = GetEnemySliders(board, opponent);

        Magics magics = Magics.Instance();
        BitBoard kingRays = magics.GetRays(kingSquare);

        sliders &= kingRays;
        if (sliders == 0)
            return [];

        List<BitBoard> pinRays = GetPossiblePinRays(sliders, kingSquare);

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

    private static List<BitBoard> GetPossiblePinRays(BitBoard sliders, int kingSquare)
    {
        List<BitBoard> pinRays = [];

        while (sliders != 0)
        {
            int sliderIndex = BitBoard.PopLSB(ref sliders);

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
            pinRays.Add(ray);
        }
        return pinRays;
    }

    // Pass BitBoard.Full for piecesToCheck to check all pieces of the player
    public static BitBoard GetRestrictionRays(BoardState board, int kingSquare, bool isWhite, BitBoard piecesToCheck)
    {
        List<BitBoard> pinRays = GetPinRays(board, kingSquare);
        BitBoard restrictionRays = BitBoard.Empty;
        BitBoard friendlyPieces = isWhite ? board.WhitePieces : board.BlackPieces;

        foreach (var ray in pinRays)
        {
            if ((ray & piecesToCheck) == 0)
                continue;

            BitBoard piecesOnRay = ray & friendlyPieces;
            if (piecesOnRay.PopCount() != 1)
                continue;

            restrictionRays |= ray;
        }
        return restrictionRays;
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