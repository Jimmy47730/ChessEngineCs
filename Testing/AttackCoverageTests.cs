using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests;

public class AttackCoverageTests
{
    [Fact]
    public void PawnCoverage_HandlesWhiteFileEdgesAndColorSelection()
    {
        var board = new Board("4k3/8/8/8/8/8/P6P/7K w - - 0 1");

        Assert.Equal(Squares("b3", "g3"), AttackCoverage.PawnCoverage(board, opponent: false));
        Assert.Equal(0ul, AttackCoverage.PawnCoverage(board));
    }

    [Fact]
    public void PawnCoverage_HandlesBlackPawnAttackDirectionsAndFileEdges()
    {
        var board = new Board("7k/8/8/8/8/8/p6p/4K3 b - - 0 1");

        Assert.Equal(Squares("b1", "g1"), AttackCoverage.PawnCoverage(board, opponent: false));
    }

    [Fact]
    public void KnightCoverage_CombinesAttacksFromEveryKnight()
    {
        var board = new Board("7k/8/8/8/3n4/8/8/4K3 w - - 0 1");

        Assert.Equal(
            Squares("b3", "f3", "c2", "e2", "c6", "e6", "b5", "f5"),
            AttackCoverage.KnightCoverage(board));
    }

    [Fact]
    public void BishopCoverage_IncludesTheFirstBlockerAndStopsBeyondIt()
    {
        var board = new Board("8/8/5p2/8/3B4/8/8/8 w - - 0 1");
        ulong expected = Squares("e5", "f6", "c5", "b6", "a7", "e3", "f2", "g1", "c3", "b2", "a1");

        Assert.Equal(expected, AttackCoverage.BishopCoverage(board, opponent: false));
    }

    [Fact]
    public void RookCoverage_IncludesTheFirstBlockerAndStopsBeyondIt()
    {
        var board = new Board("8/8/3p4/8/3R4/8/8/8 w - - 0 1");
        ulong expected = Squares(
            "d1", "d2", "d3", "d5", "d6",
            "a4", "b4", "c4", "e4", "f4", "g4", "h4");

        Assert.Equal(expected, AttackCoverage.RookCoverage(board, opponent: false));
    }

    [Fact]
    public void QueenCoverage_CombinesDiagonalAndStraightAttacks()
    {
        var board = new Board("8/8/5p2/8/3Q4/8/8/8 w - - 0 1");
        ulong expected = Squares(
            "e5", "f6", "c5", "b6", "a7", "e3", "f2", "g1", "c3", "b2", "a1",
            "d1", "d2", "d3", "d5", "d6", "d7", "d8",
            "a4", "b4", "c4", "e4", "f4", "g4", "h4");

        Assert.Equal(expected, AttackCoverage.QueenCoverage(board, opponent: false));
    }

    [Fact]
    public void KingCoverage_ReturnsAdjacentSquaresAtBoardEdge()
    {
        var board = new Board("8/8/8/8/8/8/8/K7 b - - 0 1");

        Assert.Equal(Squares("a2", "b1", "b2"), AttackCoverage.KingCoverage(board, opponent: true));
    }

    [Fact]
    public void GetFullCoverage_IsTheUnionOfEachPieceTypeCoverage()
    {
        var board = new Board("4k3/8/8/8/3B4/8/P3N3/4K3 w - - 0 1");
        ulong expected =
            AttackCoverage.PawnCoverage(board, opponent: false) |
            AttackCoverage.KnightCoverage(board, opponent: false) |
            AttackCoverage.BishopCoverage(board, opponent: false) |
            AttackCoverage.RookCoverage(board, opponent: false) |
            AttackCoverage.QueenCoverage(board, opponent: false) |
            AttackCoverage.KingCoverage(board, opponent: false);

        Assert.Equal(expected, AttackCoverage.GetFullCoverage(board, opponent: false));
    }

    private static ulong Squares(params string[] squares)
    {
        return squares.Aggregate(0ul, (bitboard, square) => bitboard | 1ul << StringRepr.SquareConversion(square));
    }
}
