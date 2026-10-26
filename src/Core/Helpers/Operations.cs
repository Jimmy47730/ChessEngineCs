using System.Diagnostics.CodeAnalysis;
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

    public static ulong SquareToU64(int square)
    {
        return 1ul << square;
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

    public static bool IsSet(ulong bitboard, int square)
    {
        return (bitboard & SquareToU64(square)) != 0;
    }

    public static ulong GetRay(ulong from, ulong to)
    {
        // The arguments are expected to each contain exactly one square.
        if (from == 0 || to == 0 || PopCount(from) != 1 || PopCount(to) != 1)
            return 0ul;

        int fromIndex = LsbIndex(from);
        int toIndex = LsbIndex(to);
        int fromRank = fromIndex / 8;
        int fromFile = fromIndex % 8;
        int toRank = toIndex / 8;
        int toFile = toIndex % 8;
        int rankDifference = toRank - fromRank;
        int fileDifference = toFile - fromFile;

        if (fromFile != toFile && fromRank != toRank && Math.Abs(rankDifference) != Math.Abs(fileDifference))
            return 0ul;

        

        int rankStep = Math.Sign(rankDifference);
        int fileStep = Math.Sign(fileDifference);
        ulong ray = 0ul;
        int rank = fromRank;
        int file = fromFile;

        while (true)
        {
            ray |= 1ul << (rank * 8 + file);
            if (rank == toRank && file == toFile)
                return ray;

            rank += rankStep;
            file += fileStep;
        }
    }

    
}