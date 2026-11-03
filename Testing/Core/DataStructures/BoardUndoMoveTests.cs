using Core.DataStructures;
using Core.Helpers;
using Xunit;

namespace ChessEngine.Tests.Core.DataStructures;

public class BoardUndoMoveTests
{
    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e2", "e4", 1)]
    [InlineData("4k3/8/8/3p4/4P3/8/8/4K3 w - - 7 3", "e4", "d5", 4)]
    [InlineData("4k3/P7/8/8/8/8/8/4K3 w - - 0 1", "a7", "a8", 11)]
    [InlineData("1r2k3/P7/8/8/8/8/8/4K3 w - - 0 1", "a7", "b8", 15)]
    [InlineData("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5", "d6", 5)]
    [InlineData("4k3/8/8/8/8/8/8/4K2R w K - 0 1", "e1", "g1", 2)]
    [InlineData("r3k3/8/8/8/8/8/8/R3K3 b Qq - 12 8", "a8", "a1", 4)]
    public void UndoMove_RestoresOriginalPosition(
        string fen,
        string from,
        string to,
        int flags)
    {
        var board = new Board(fen);
        ulong[] originalBitboards = Enumerable.Range(0, 12)
            .Select(board.GetBitboard)
            .ToArray();
        ulong originalHash = board.zobristHash;
        var move = new Move(Square(from), Square(to), flags);

        UndoInfo undoInfo = board.ApplyMove(move);
        board.UndoMove(move, undoInfo);

        Assert.Equal(fen, StringRepr.ToFen(board));
        Assert.Equal(originalHash, board.zobristHash);
        Assert.Equal(originalBitboards, Enumerable.Range(0, 12).Select(board.GetBitboard).ToArray());
    }

    [Fact]
    public void UndoMove_RestoresPositionsInReverseMoveOrder()
    {
        var board = new Board();
        string initialFen = StringRepr.ToFen(board);
        ulong initialHash = board.zobristHash;

        Move whiteMove = new(Square("e2"), Square("e4"), Move.GenerateSpecialMoveFlag(true));
        UndoInfo whiteUndo = board.ApplyMove(whiteMove);
        Move blackMove = new(Square("e7"), Square("e5"), Move.GenerateSpecialMoveFlag(true));
        UndoInfo blackUndo = board.ApplyMove(blackMove);

        board.UndoMove(blackMove, blackUndo);
        board.UndoMove(whiteMove, whiteUndo);

        Assert.Equal(initialFen, StringRepr.ToFen(board));
        Assert.Equal(initialHash, board.zobristHash);
    }

    private static int Square(string name) => StringRepr.SquareConversion(name);
}
