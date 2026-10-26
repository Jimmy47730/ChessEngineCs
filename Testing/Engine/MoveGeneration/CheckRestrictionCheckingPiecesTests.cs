using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class CheckRestrictionCheckingPiecesTests
{
    [Fact]
    public void GetCheckingPieces_ReturnsEmptyWhenKingIsNotInCheck()
    {
        var board = new Board("4k3/8/8/8/8/8/8/4K3 w - - 0 1");

        Assert.Equal(0ul, CheckRestriction.GetCheckingPieces(board));
    }

    [Fact]
    public void GetCheckingPieces_ReturnsEmptyWhenSideToMoveHasNoKing()
    {
        var board = new Board("4k3/8/8/8/8/8/8/8 w - - 0 1");

        Assert.Equal(0ul, CheckRestriction.GetCheckingPieces(board));
    }

    [Theory]
    [InlineData("4r1k1/8/8/8/8/8/8/4K3 w - - 0 1", "e8")]
    [InlineData("7k/8/8/8/8/5n2/8/4K3 w - - 0 1", "f3")]
    [InlineData("7k/8/8/8/7b/8/8/4K3 w - - 0 1", "h4")]
    [InlineData("4q2k/8/8/8/8/8/8/4K3 w - - 0 1", "e8")]
    [InlineData("7k/8/8/8/8/8/3p4/4K3 w - - 0 1", "d2")]
    public void GetCheckingPieces_ReturnsTheCheckingSliderKnightOrPawn(string fen, string checkingSquare)
    {
        Assert.Equal(Square(checkingSquare), CheckRestriction.GetCheckingPieces(new Board(fen)));
    }

    [Fact]
    public void GetCheckingPieces_DetectsWhitePawnCheckAgainstBlackKing()
    {
        var board = new Board("3k4/4P3/8/8/8/8/8/K7 b - - 0 1");

        Assert.Equal(Square("e7"), CheckRestriction.GetCheckingPieces(board));
    }

    [Fact]
    public void GetCheckingPieces_DoesNotSeeSliderThroughBlockingPiece()
    {
        var board = new Board("4r1k1/8/8/8/4P3/8/8/4K3 w - - 0 1");

        Assert.Equal(0ul, CheckRestriction.GetCheckingPieces(board));
    }

    [Fact]
    public void GetCheckingPieces_ReturnsBothPawnsWhenTheyDeliverDoubleCheck()
    {
        var board = new Board("7k/8/8/8/8/8/3p1p2/4K3 w - - 0 1");

        Assert.Equal(Squares("d2", "f2"), CheckRestriction.GetCheckingPieces(board));
    }

    [Fact]
    public void GetCheckingPieces_ReturnsEveryPieceInDoubleCheck()
    {
        var board = new Board("4r1k1/8/8/8/8/8/3p4/4K3 w - - 0 1");

        Assert.Equal(Squares("e8", "d2"), CheckRestriction.GetCheckingPieces(board));
    }

    private static ulong Square(string square) => Squares(square);

    private static ulong Squares(params string[] squares)
    {
        return squares.Aggregate(0ul, (bitboard, square) => bitboard | 1ul << StringRepr.SquareConversion(square));
    }
}
