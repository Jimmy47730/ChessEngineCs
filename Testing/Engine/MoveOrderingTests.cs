using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Engine.Search;
using Xunit;

namespace ChessEngine.Tests.Engine;

public class MoveOrderingTests
{
    [Fact]
    public void OrderMoves_PrioritizesCheckingMovesAndRestoresBoard()
    {
        var board = new Board("k7/8/8/8/8/8/4R3/7K w - - 0 1");
        List<Move> moves = MoveGenerator.GenerateMoves(board);
        ulong originalHash = board.zobristHash;
        string originalFen = StringRepr.ToFen(board);

        MoveOrdering.OrderMoves(board, moves);

        Assert.Equal("e2a2", StringRepr.MoveToUci(moves[0]));
        Assert.Equal(originalHash, board.zobristHash);
        Assert.Equal(originalFen, StringRepr.ToFen(board));
    }

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
