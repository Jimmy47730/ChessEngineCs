

namespace Core.Helpers;

public static class Operations
{
    /*
        This class adds helper methods to isolate various operations, such as square number calculations and bitboard manipulations.
    */

    public static int SquareIndex(int rank, int file)
    {
        /*
            Converts a rank and file to a square index (0-63).
            The conversion is based on the position of the square on the chessboard:
            - 'a1' corresponds to 0
            - 'h8' corresponds to 63
        */
        if (rank < 0 || rank > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(rank));
        }
        if (file < 0 || file > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(file));
        }
        return rank * 8 + file;
    }
    
}