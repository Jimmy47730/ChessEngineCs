using Core.Helpers;
using Xunit;

namespace ChessEngine.Tests.Core.Helpers;

public class OperationsTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 7, 7)]
    [InlineData(7, 0, 56)]
    [InlineData(7, 7, 63)]
    [InlineData(3, 4, 28)]
    public void SquareIndex_ConvertsRankAndFile(int rank, int file, int expected)
    {
        Assert.Equal(expected, Operations.SquareIndex(rank, file));
    }

    [Theory]
    [InlineData(-1, 0, "rank")]
    [InlineData(8, 0, "rank")]
    [InlineData(0, -1, "file")]
    [InlineData(0, 8, "file")]
    public void SquareIndex_RejectsCoordinatesOutsideBoard(int rank, int file, string parameterName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Operations.SquareIndex(rank, file));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Theory]
    [InlineData(1ul, 0, 1ul)]
    [InlineData(1ul, 3, 8ul)]
    [InlineData(1ul, 63, 0x8000000000000000ul)]
    [InlineData(8ul, -3, 1ul)]
    [InlineData(0x8000000000000000ul, -63, 1ul)]
    [InlineData(0x8000000000000000ul, 1, 0ul)]
    [InlineData(1ul, 64, 0ul)]
    [InlineData(1ul, -64, 0ul)]
    public void ShiftByInt_ShiftsWithinBoundsAndDropsOutOfRangeShifts(ulong bitboard, int shift, ulong expected)
    {
        Assert.Equal(expected, Operations.ShiftByInt(bitboard, shift));
    }

    [Theory]
    [InlineData(0ul, -1)]
    [InlineData(1ul, 0)]
    [InlineData(0x8000000000000000ul, 63)]
    [InlineData(0x8000000000000100ul, 8)]
    public void LsbIndex_ReturnsLeastSignificantSetBitOrMinusOne(ulong bitboard, int expected)
    {
        Assert.Equal(expected, Operations.LsbIndex(bitboard));
    }

    [Fact]
    public void ClearBit_RemovesOnlyTheRequestedBit()
    {
        ulong bitboard = (1ul << 2) | (1ul << 31) | (1ul << 63);

        Assert.Equal((1ul << 2) | (1ul << 63), Operations.ClearBit(bitboard, 31));
        Assert.Equal(bitboard, Operations.ClearBit(bitboard, 10));
    }

    [Fact]
    public void PopLSB_RemovesBitsInAscendingOrderAndReturnsMinusOneForEmptyBoard()
    {
        ulong bitboard = (1ul << 5) | (1ul << 32) | (1ul << 63);

        Assert.Equal(5, Operations.PopLSB(ref bitboard));
        Assert.Equal((1ul << 32) | (1ul << 63), bitboard);
        Assert.Equal(32, Operations.PopLSB(ref bitboard));
        Assert.Equal(1ul << 63, bitboard);
        Assert.Equal(63, Operations.PopLSB(ref bitboard));
        Assert.Equal(0ul, bitboard);
        Assert.Equal(-1, Operations.PopLSB(ref bitboard));
    }

    [Theory]
    [InlineData(0ul, 0)]
    [InlineData(1ul, 1)]
    [InlineData(0x8000000000000000ul, 1)]
    [InlineData(0x8000000000000101ul, 3)]
    [InlineData(0xfffffffffffffffful, 64)]
    public void PopCount_ReturnsTheNumberOfSetBits(ulong bitboard, int expected)
    {
        Assert.Equal(expected, Operations.PopCount(bitboard));
    }

    [Theory]
    [InlineData(0b1010ul, 0, false)]
    [InlineData(0b1010ul, 3, true)]
    [InlineData(0x8000000000000000ul, 63, true)]
    [InlineData(0x8000000000000000ul, 0, false)]
    public void IsSet_ReportsWhetherTheSquareBitIsSet(ulong bitboard, int square, bool expected)
    {
        Assert.Equal(expected, Operations.IsSet(bitboard, square));
    }

    [Theory]
    [InlineData(0, 7, 0x00000000000000FFul)]
    [InlineData(8, 15, 0x000000000000FF00ul)]
    [InlineData(15, 8, 0x000000000000FF00ul)]
    [InlineData(0, 56, 0x0101010101010101ul)]
    [InlineData(7, 63, 0x8080808080808080ul)]
    [InlineData(63, 7, 0x8080808080808080ul)]
    [InlineData(9, 57, 0x0202020202020200ul)]
    [InlineData(0, 63, 0x8040201008040201ul)]
    [InlineData(63, 0, 0x8040201008040201ul)]
    [InlineData(27, 27, 1ul << 27)]
    public void GetRay_ReturnsInclusiveRayForAlignedSquares(int fromSquare, int toSquare, ulong expected)
    {
        Assert.Equal(
            expected,
            Operations.GetRay(Operations.SquareToU64(fromSquare), Operations.SquareToU64(toSquare)));
    }

    [Theory]
    [InlineData(0ul, 1ul)]
    [InlineData(1ul, 0ul)]
    [InlineData(3ul, 1ul)]
    [InlineData(1ul, 3ul)]
    public void GetRay_ReturnsEmptyForInputsThatAreNotSingleSquares(ulong from, ulong to)
    {
        Assert.Equal(0ul, Operations.GetRay(from, to));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(3, 20)]
    [InlineData(6, 56)]
    public void GetRay_ReturnsEmptyForUnalignedSquares(int fromSquare, int toSquare)
    {
        Assert.Equal(
            0ul,
            Operations.GetRay(Operations.SquareToU64(fromSquare), Operations.SquareToU64(toSquare)));
    }
}
