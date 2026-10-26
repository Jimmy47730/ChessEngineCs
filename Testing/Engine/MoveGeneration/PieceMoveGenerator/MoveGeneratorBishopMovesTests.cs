using System.Reflection;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class MoveGeneratorBishopMovesTests
{
    [Fact]
    public void GenerateBishopMoves_GeneratesAllDiagonalDestinationsFromCenter()
    {
        var board = new Board("8/8/8/8/3B4/8/8/8 w - - 0 1");
        string[] destinations =
        [
            "a1", "b2", "c3", "e5", "f6", "g7", "h8",
            "a7", "b6", "c5", "e3", "f2", "g1"
        ];

        var moves = GenerateBishopMoves(board);

        Assert.Equal(13, moves.Count);
    }

    [Fact]
    public void GenerateBishopMoves_StopsAtFriendlyBlockerAndCanCaptureEnemyBlocker()
    {
        var board = new Board("8/8/5P2/8/3B4/8/1p6/8 w - - 0 1");

        var moves = GenerateBishopMoves(board);

        Assert.DoesNotContain(moves, move => move.To() == Square("f6"));
        Assert.DoesNotContain(moves, move => move.To() == Square("g7"));
        Assert.Contains(moves, move => move.To() == Square("b2") &&
            move.Flags() == Move.GenerateSpecialMoveFlag(CaptureType.Normal));
        Assert.DoesNotContain(moves, move => move.To() == Square("a1"));
    }

    [Fact]
    public void GenerateBishopMoves_GeneratesMovesFromBoardEdgeWithoutWrapping()
    {
        var board = new Board("8/8/8/8/8/8/8/B7 w - - 0 1");

        var moves = GenerateBishopMoves(board);

        Assert.Equal(
            Enumerable.Range(1, 7).Select(rank => ExpectedMove("a1", $"{(char)('a' + rank)}{rank + 1}"))
                .OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateBishopMoves_RestrictsDestinationsWhenBlockingCheck()
    {
        var board = new Board("8/8/8/3B4/8/8/8/8 w - - 0 1");

        var moves = GenerateBishopMoves(board, SquareBit("e6"));

        Assert.Equal(new[] { ExpectedMove("d5", "e6") }, moves.Select(ToResult));
    }

    [Fact]
    public void GenerateBishopMoves_AllowsCaptureAlongDiagonalPin()
    {
        var board = new Board("4k3/8/8/8/8/2b5/3B4/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("c3"));

        var moves = GenerateBishopMoves(board, pinRays: [pinRay]);

        Assert.Equal(new[] { ExpectedMove("d2", "c3", 4) }, moves.Select(ToResult));
    }

    [Fact]
    public void GenerateBishopMoves_DoesNotMoveOffStraightPinRay()
    {
        var board = new Board("4r1k1/8/8/8/8/8/4B3/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("e8"));

        var moves = GenerateBishopMoves(board, pinRays: [pinRay]);

        Assert.Empty(moves);
    }

    private static List<Move> GenerateBishopMoves(
        Board board,
        ulong allowedDestinations = ulong.MaxValue,
        List<ulong>? pinRays = null)
    {
        MethodInfo method = typeof(MoveGenerator).GetMethod(
            "GenerateBishopMoves",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateBishopMoves method was not found.");

        return (List<Move>)method.Invoke(null, [board, allowedDestinations, pinRays ?? []])!;
    }

    private static MoveResult ToResult(Move move) => new(move.From(), move.To(), move.Flags());

    private static MoveResult ExpectedMove(string from, string to, int flags = 0) =>
        new(Square(from), Square(to), flags);

    private static ulong SquareBit(string square) => Operations.SquareToU64(Square(square));

    private static int Square(string square) => StringRepr.SquareConversion(square);

    private readonly record struct MoveResult(int From, int To, int Flags);
}
