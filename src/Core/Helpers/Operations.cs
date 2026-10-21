using System.Numerics;

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

    public static ulong ShiftByInt(ulong bitboard, int n)
    {
        if (n > 63 | n < -63)
            return 0ul;
        else if (n < 0)
            return bitboard >> -n;
        else if (n == 0)
            return bitboard;
        return bitboard << n;
    }

    // Returns index of least-significant set bit, or -1 if empty
    public static int LsbIndex(ulong bitboard)
    {
        if (bitboard == 0) return -1;
        return BitOperations.TrailingZeroCount(bitboard);
    }

    public static ulong ClearBit(ulong bitboard, int bit)
    {
        return bitboard & ~(1ul << bit);
    }

    public static int PopLSB(ref ulong bitboard)
    {
        int lsbIndex = LsbIndex(bitboard);
        if (lsbIndex == -1) return -1;
        bitboard = ClearBit(bitboard, lsbIndex);
        return lsbIndex;
    }

    public static int PopCount(ulong bitboard)
    {
        int count = 0;
        while (PopLSB(ref bitboard) != -1)
            count++;

        return count;
    }

    
}