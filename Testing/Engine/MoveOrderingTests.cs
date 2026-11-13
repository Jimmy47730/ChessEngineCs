using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Engine.Search;
using Xunit;

namespace ChessEngine.Tests.Engine;

public class MoveOrderingTests
{

    [Fact]
    public void OrderMoves_PrioritizesQueenPromotion()
    {
        var board = new Board("7k/P7/8/8/8/8/8/4K3 w - - 0 1");
        List<Move> moves = MoveGenerator.GenerateMoves(board);

        MoveOrdering.OrderMoves(board, moves);

        Assert.Equal("a7a8q", StringRepr.MoveToUci(moves[0]));
    }

    [Fact]
    public void OrderMoves_PrioritizesCapturesByVictimValue()
    {
        var board = new Board("7k/8/8/1p1q4/8/2N5/8/K7 w - - 0 1");
        List<Move> moves = MoveGenerator.GenerateMoves(board);

        MoveOrdering.OrderMoves(board, moves);

        Assert.Equal("c3d5", StringRepr.MoveToUci(moves[0]));
    }
}
