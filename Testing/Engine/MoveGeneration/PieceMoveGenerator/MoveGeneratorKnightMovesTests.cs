using System.Reflection;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class MoveGeneratorKnightMovesTests
{
    [Theory]
    [InlineData("8/8/8/8/3N4/8/8/8 w - - 0 1", "d4", "c2", "e2", "b3", "f3", "b5", "f5", "c6", "e6")]
    [InlineData("8/8/8/8/8/8/8/N7 w - - 0 1", "a1", "b3", "c2")]
    public void GenerateKnightMoves_GeneratesAllOnBoardDestinations(string fen, string from, params string[] destinations)
    {
        var moves = GenerateKnightMoves(new Board(fen));

        Assert.Equal(
            destinations.Select(to => ExpectedMove(from, to)).OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateKnightMoves_ExcludesFriendlySquaresAndMarksCaptures()
    {
        var board = new Board("8/8/2P5/5p2/3N4/8/8/8 w - - 0 1");

        var moves = GenerateKnightMoves(board);

        Assert.DoesNotContain(moves, move => move.To() == Square("c6"));
        Assert.Contains(moves, move => move.To() == Square("f5") &&
            move.Flags() == Move.GenerateSpecialMoveFlag(CaptureType.Normal));
        Assert.Equal(7, moves.Count);
    }

    [Fact]
    public void GenerateKnightMoves_RestrictsMovesToAllowedDestinations()
    {
        var board = new Board("8/8/8/8/3N4/8/8/8 w - - 0 1");
        ulong allowedDestinations = SquareBit("c6") | SquareBit("f5");

        var moves = GenerateKnightMoves(board, allowedDestinations);

        Assert.Equal(
            new[] { ExpectedMove("d4", "c6"), ExpectedMove("d4", "f5") }.OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Theory]
    [InlineData("4k3/8/8/8/8/2b5/3N4/4K3 w - - 0 1", "e1", "c3", "d2")]
    [InlineData("r6k/8/8/8/8/8/N7/K7 w - - 0 1", "a1", "a8", "a2")]
    public void GenerateKnightMoves_DoesNotMovePinnedKnight(
        string fen,
        string kingSquare,
        string attackerSquare,
        string knightSquare)
    {
        var board = new Board(fen);
        ulong pinRay = Operations.GetRay(SquareBit(kingSquare), SquareBit(attackerSquare));

        var moves = GenerateKnightMoves(board, pinRays: [pinRay]);

        Assert.DoesNotContain(moves, move => move.From() == Square(knightSquare));
    }

    private static List<Move> GenerateKnightMoves(
        Board board,
        ulong allowedDestinations = ulong.MaxValue,
        List<ulong>? pinRays = null)
    {
        MethodInfo method = typeof(MoveGenerator).GetMethod(
            "GenerateKnightMoves",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateKnightMoves method was not found.");

        return (List<Move>)method.Invoke(null, [board, allowedDestinations, pinRays ?? []])!;
    }

    private static MoveResult ToResult(Move move) => new(move.From(), move.To(), move.Flags());

    private static MoveResult ExpectedMove(string from, string to) => new(Square(from), Square(to), 0);

    private static ulong SquareBit(string square) => Operations.SquareToU64(Square(square));

    private static int Square(string square) => StringRepr.SquareConversion(square);

    private readonly record struct MoveResult(int From, int To, int Flags);
}
