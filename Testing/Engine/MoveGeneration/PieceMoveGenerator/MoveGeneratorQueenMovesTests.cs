using System.Reflection;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class MoveGeneratorQueenMovesTests
{
    [Fact]
    public void GenerateQueenMoves_GeneratesDiagonalAndStraightDestinationsFromCenter()
    {
        var board = new Board("8/8/8/8/3Q4/8/8/8 w - - 0 1");

        var moves = GenerateQueenMoves(board);

        Assert.Equal(27, moves.Count);
        Assert.Contains(moves, move => move.From() == Square("d4") && move.To() == Square("d8"));
        Assert.Contains(moves, move => move.From() == Square("d4") && move.To() == Square("h4"));
        Assert.Contains(moves, move => move.From() == Square("d4") && move.To() == Square("h8"));
        Assert.Contains(moves, move => move.From() == Square("d4") && move.To() == Square("a1"));
    }

    [Fact]
    public void GenerateQueenMoves_StopsAtFriendlyBlockerAndCanCaptureEnemyBlocker()
    {
        var board = new Board("8/8/5P2/8/3Q1p2/8/1p6/8 w - - 0 1");

        var moves = GenerateQueenMoves(board);

        Assert.DoesNotContain(moves, move => move.To() == Square("f6"));
        Assert.DoesNotContain(moves, move => move.To() == Square("g7"));
        Assert.Contains(moves, move => move.To() == Square("f4") &&
            move.Flags() == Move.GenerateSpecialMoveFlag(CaptureType.Normal));
        Assert.DoesNotContain(moves, move => move.To() == Square("g4"));
        Assert.Contains(moves, move => move.To() == Square("b2") &&
            move.Flags() == Move.GenerateSpecialMoveFlag(CaptureType.Normal));
        Assert.DoesNotContain(moves, move => move.To() == Square("a1"));
    }

    [Fact]
    public void GenerateQueenMoves_RestrictsDestinationsToCheckEvasionSquares()
    {
        var board = new Board("8/8/8/3Q4/8/8/8/8 w - - 0 1");

        var moves = GenerateQueenMoves(board, SquareBit("d7") | SquareBit("f5"));

        Assert.Equal(
            new[] { ExpectedMove("d5", "d7"), ExpectedMove("d5", "f5") }.OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateQueenMoves_CanMoveAlongStraightPinRayAndCapturePinner()
    {
        var board = new Board("4r1k1/8/8/8/8/8/4Q3/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("e8"));
        string[] destinations = ["e3", "e4", "e5", "e6", "e7", "e8"];

        var moves = GenerateQueenMoves(board, pinRays: [pinRay]);

        Assert.Equal(
            destinations.Select(to => ExpectedMove("e2", to, to == "e8" ? 4 : 0)).OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateQueenMoves_CanMoveAlongDiagonalPinRayAndCapturePinner()
    {
        var board = new Board("4k3/8/8/8/8/2b5/3Q4/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("c3"));

        var moves = GenerateQueenMoves(board, pinRays: [pinRay]);

        Assert.Equal(
            new[] { ExpectedMove("d2", "c3", 4) },
            moves.Select(ToResult));
    }

    private static List<Move> GenerateQueenMoves(
        Board board,
        ulong allowedDestinations = ulong.MaxValue,
        List<ulong>? pinRays = null)
    {
        MethodInfo method = typeof(MoveGenerator).GetMethod(
            "GenerateQueenMoves",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateQueenMoves method was not found.");

        return (List<Move>)method.Invoke(null, [board, allowedDestinations, pinRays ?? []])!;
    }

    private static MoveResult ToResult(Move move) => new(move.From(), move.To(), move.Flags());

    private static MoveResult ExpectedMove(string from, string to, int flags = 0) =>
        new(Square(from), Square(to), flags);

    private static ulong SquareBit(string square) => Operations.SquareToU64(Square(square));

    private static int Square(string square) => StringRepr.SquareConversion(square);

    private readonly record struct MoveResult(int From, int To, int Flags);
}
