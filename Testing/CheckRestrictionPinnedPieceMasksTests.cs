using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests;

public class CheckRestrictionPinnedPieceMasksTests
{
    [Fact]
    public void GetPinnedPieceMasks_ReturnsEmptyWhenThereIsNoKing()
    {
        var board = new Board("4r2k/8/8/8/8/8/4N3/8 w - - 0 1");

        Assert.Empty(CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_ReturnsEmptyWhenNoPieceIsPinned()
    {
        var board = new Board("4r2k/8/8/8/8/8/8/4K3 w - - 0 1");

        Assert.Empty(CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_FindsVerticalRookPinAndIncludesSliderButNotKing()
    {
        var board = new Board("4r2k/8/8/8/8/8/4N3/4K3 w - - 0 1");
        ulong expectedMask = Squares("e8", "e7", "e6", "e5", "e4", "e3", "e2");

        Assert.Equal(new[] { expectedMask }, CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_FindsDiagonalBishopPin()
    {
        var board = new Board("7k/8/8/8/7b/8/5N2/4K3 w - - 0 1");
        ulong expectedMask = Squares("h4", "g3", "f2");

        Assert.Equal(new[] { expectedMask }, CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_FindsMultiplePinsInDifferentDirections()
    {
        var board = new Board("4r2k/8/8/b7/8/8/3BN3/4K3 w - - 0 1");
        ulong verticalMask = Squares("e8", "e7", "e6", "e5", "e4", "e3", "e2");
        ulong diagonalMask = Squares("a5", "b4", "c3", "d2");

        Assert.Equal(
            new[] { verticalMask, diagonalMask }.Order(),
            CheckRestriction.GetPinnedPieceMasks(board).Order());
    }

    [Fact]
    public void GetPinnedPieceMasks_StopsAtSecondFriendlyPiece()
    {
        var board = new Board("4r2k/8/8/8/8/4B3/4N3/4K3 w - - 0 1");

        Assert.Empty(CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_DoesNotTreatAnIncompatibleEnemyPieceAsAPinner()
    {
        var board = new Board("4b2k/8/8/8/8/8/4N3/4K3 w - - 0 1");

        Assert.Empty(CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_DoesNotSeeSliderPastAnEnemyBlocker()
    {
        var board = new Board("4r2k/8/8/8/4p3/8/4N3/4K3 w - - 0 1");

        Assert.Empty(CheckRestriction.GetPinnedPieceMasks(board));
    }

    [Fact]
    public void GetPinnedPieceMasks_FindsPinAlongDiagonalFromCornerKing()
    {
        var board = new Board("7b/8/8/8/8/8/1N6/K7 w - - 0 1");
        ulong expectedMask = Squares("h8", "g7", "f6", "e5", "d4", "c3", "b2");

        Assert.Equal(new[] { expectedMask }, CheckRestriction.GetPinnedPieceMasks(board));
    }

    private static ulong Squares(params string[] squares)
    {
        return squares.Aggregate(0ul, (bitboard, square) => bitboard | 1ul << StringRepr.SquareConversion(square));
    }
}
