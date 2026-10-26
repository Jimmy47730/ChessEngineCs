using System.Reflection;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class MoveGeneratorRookMovesTests
{
    [Fact]
    public void GenerateRookMoves_GeneratesAllStraightDestinationsFromCenter()
    {
        var board = new Board("8/8/8/8/3R4/8/8/8 w - - 0 1");
        string[] destinations =
        [
            "d1", "d2", "d3", "d5", "d6", "d7", "d8",
            "a4", "b4", "c4", "e4", "f4", "g4", "h4"
        ];

        var moves = GenerateRookMoves(board);

        Assert.Equal(14, moves.Count);
    }

    [Fact]
    public void GenerateRookMoves_StopsAtFriendlyBlockerAndCanCaptureEnemyBlocker()
    {
        var board = new Board("8/8/3P4/8/3R1p2/8/8/8 w - - 0 1");

        var moves = GenerateRookMoves(board);

        Assert.DoesNotContain(moves, move => move.To() == Square("d6"));
        Assert.DoesNotContain(moves, move => move.To() == Square("d7"));
        Assert.Contains(moves, move => move.To() == Square("f4") &&
            move.Flags() == Move.GenerateSpecialMoveFlag(CaptureType.Normal));
        Assert.DoesNotContain(moves, move => move.To() == Square("g4"));
    }

    [Fact]
    public void GenerateRookMoves_GeneratesMovesFromBoardEdgeWithoutWrapping()
    {
        var board = new Board("8/8/8/8/8/8/8/R7 w - - 0 1");
        string[] destinations =
        [
            "a2", "a3", "a4", "a5", "a6", "a7", "a8",
            "b1", "c1", "d1", "e1", "f1", "g1", "h1"
        ];

        var moves = GenerateRookMoves(board);

        Assert.Equal(
            destinations.Select(to => ExpectedMove("a1", to)).OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateRookMoves_RestrictsDestinationsToCheckEvasionSquares()
    {
        var board = new Board("8/8/8/3R4/8/8/8/8 w - - 0 1");

        var moves = GenerateRookMoves(board, SquareBit("d7") | SquareBit("f5"));

        Assert.Equal(
            new[] { ExpectedMove("d5", "d7"), ExpectedMove("d5", "f5") }.OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateRookMoves_CanMoveAlongStraightPinRayAndCapturePinner()
    {
        var board = new Board("4r1k1/8/8/8/8/8/4R3/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("e8"));
        string[] destinations = ["e3", "e4", "e5", "e6", "e7", "e8"];

        var moves = GenerateRookMoves(board, pinRays: [pinRay]);

        Assert.Equal(
            destinations.Select(to => ExpectedMove("e2", to, to == "e8" ? 4 : 0)).OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateRookMoves_DoesNotMoveOffDiagonalPinRay()
    {
        var board = new Board("4k3/8/8/8/8/2b5/3R4/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("c3"));

        Assert.Empty(GenerateRookMoves(board, pinRays: [pinRay]));
    }

    private static List<Move> GenerateRookMoves(
        Board board,
        ulong allowedDestinations = ulong.MaxValue,
        List<ulong>? pinRays = null)
    {
        MethodInfo method = typeof(MoveGenerator).GetMethod(
            "GenerateRookMoves",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateRookMoves method was not found.");

        return (List<Move>)method.Invoke(null, [board, allowedDestinations, pinRays ?? []])!;
    }

    private static MoveResult ToResult(Move move) => new(move.From(), move.To(), move.Flags());

    private static MoveResult ExpectedMove(string from, string to, int flags = 0) =>
        new(Square(from), Square(to), flags);

    private static ulong SquareBit(string square) => Operations.SquareToU64(Square(square));

    private static int Square(string square) => StringRepr.SquareConversion(square);

    private readonly record struct MoveResult(int From, int To, int Flags);
}
