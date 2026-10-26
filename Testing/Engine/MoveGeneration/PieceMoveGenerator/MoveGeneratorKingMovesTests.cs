using System.Reflection;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class MoveGeneratorKingMovesTests
{
    [Theory]
    [InlineData("4k3/8/8/8/8/8/8/K7 w - - 0 1", "a1", "a2", "b1", "b2")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - - 0 1", "e1", "d1", "f1", "d2", "e2", "f2")]
    [InlineData("4k3/8/8/8/3K4/8/8/8 w - - 0 1", "d4", "c3", "d3", "e3", "c4", "e4", "c5", "d5", "e5")]
    public void GenerateKingMoves_GeneratesOnlyOnBoardAdjacentSquares(string fen, string from, params string[] destinations)
    {
        var moves = GenerateKingMoves(new Board(fen));

        Assert.Equal(
            destinations.Select(to => Move(from, to)).OrderBy(move => move.To),
            moves.Select(ToResult).OrderBy(move => move.To));
    }

    [Fact]
    public void GenerateKingMoves_ExcludesSquaresOccupiedByFriendlyPieces()
    {
        var board = new Board("7k/8/8/8/4P3/4K3/8/8 w - - 0 1");

        var moves = GenerateKingMoves(board);

        Assert.DoesNotContain(moves, move => move.To() == Square("e4"));
        Assert.Equal(7, moves.Count);
    }

    [Fact]
    public void GenerateKingMoves_ExcludesAttackedSquares()
    {
        var board = new Board("3r1r1k/8/8/8/8/8/8/4K3 w - - 0 1");

        var moves = GenerateKingMoves(board);

        Assert.DoesNotContain(moves, move => move.To() == Square("d1"));
        Assert.DoesNotContain(moves, move => move.To() == Square("f1"));
    }

    [Fact]
    public void GenerateKingMoves_ExcludesSquaresOccupiedByBlackFriendlyPieces()
    {
        var board = new Board("6rk/8/8/8/8/8/8/K7 b - - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.To() == Square("g8"));
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCaptureAnOpponentPieceProtectedByAnotherPiece()
    {
        var board = new Board("3r3k/8/8/8/3n4/3K4/8/8 w - - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.To() == Square("d4"));
    }

    [Fact]
    public void GenerateKingMoves_MarksCapturedPieceWithoutMarkingFollowingQuietMoves()
    {
        var board = new Board("7k/8/8/8/3n4/4K3/8/8 w - - 0 1");

        var moves = GenerateKingMoves(board);

        Assert.Contains(moves, move => move.To() == Square("d4") && move.Flags() == 4);
        Assert.Contains(moves, move => move.To() == Square("d3") && move.Flags() == 0);
    }

    [Fact]
    public void GenerateKingMoves_ReturnsNoMovesWhenNoKingExists()
    {
        var board = new Board("7k/8/8/8/8/8/8/8 w - - 0 1");

        Assert.Empty(GenerateKingMoves(board));
    }

    [Fact]
    public void GenerateKingMoves_ReturnsEmptyWhenEveryAdjacentSquareIsFriendlyOccupied()
    {
        var board = new Board("7k/8/8/8/8/8/PP6/KP6 w - - 0 1");

        Assert.Empty(GenerateKingMoves(board));
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCastleWhileInCheck()
    {
        var board = new Board("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");

        var moves = GenerateKingMoves(board, isInCheck: true);

        Assert.DoesNotContain(moves, move => move.IsCastling());
    }

    [Theory]
    [InlineData("w", "e1", "g1", "e1", "c1")]
    [InlineData("b", "e8", "g8", "e8", "c8")]
    public void GenerateKingMoves_GeneratesBothCastlesWithCorrectFlags(
        string sideToMove,
        string kingStart,
        string kingSideDestination,
        string queenStart,
        string queenSideDestination)
    {
        var board = new Board($"r3k2r/8/8/8/8/8/8/R3K2R {sideToMove} KQkq - 0 1");
        var moves = GenerateKingMoves(board);

        Assert.Contains(moves, move =>
            move.From() == Square(kingStart) &&
            move.To() == Square(kingSideDestination) &&
            move.IsKingSideCastle());
        Assert.Contains(moves, move =>
            move.From() == Square(queenStart) &&
            move.To() == Square(queenSideDestination) &&
            move.IsQueenSideCastle());
    }

    [Theory]
    [InlineData("K", true, false)]
    [InlineData("Q", false, true)]
    public void GenerateKingMoves_RespectsEachCastlingRightIndependently(
        string rights,
        bool expectKingSide,
        bool expectQueenSide)
    {
        var board = new Board($"r3k2r/8/8/8/8/8/8/R3K2R w {rights} - 0 1");

        var moves = GenerateKingMoves(board);

        Assert.Equal(expectKingSide, moves.Any(move => move.IsKingSideCastle()));
        Assert.Equal(expectQueenSide, moves.Any(move => move.IsQueenSideCastle()));
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCastleWithoutCastlingRights()
    {
        var board = new Board("r3k2r/8/8/8/8/8/8/R3K2R w - - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsCastling());
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCastleWhenTheRequiredRookIsMissing()
    {
        var board = new Board("4k3/8/8/8/8/8/8/4K3 w KQ - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsCastling());
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCastleWhenSquaresBetweenKingAndRookAreOccupied()
    {
        var board = new Board("4k3/8/8/8/8/8/8/4KN1R w K - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsCastling());
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCastleThroughAnAttackedSquare()
    {
        var board = new Board("5r1k/8/8/8/8/8/8/4K2R w K - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsCastling());
    }

    [Fact]
    public void GenerateKingMoves_DoesNotQueenSideCastleThroughAnAttackedSquare()
    {
        var board = new Board("k7/8/8/8/8/8/8/r3K2R w KQ - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsQueenSideCastle());
    }

    [Fact]
    public void GenerateKingMoves_DoesNotQueenSideCastleWhenTheExtraClearanceSquareIsOccupied()
    {
        var board = new Board("4k3/8/8/8/8/8/8/RN2K3 w Q - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsQueenSideCastle());
    }

    [Fact]
    public void GenerateKingMoves_DoesNotCastleFromANonHomeKingSquare()
    {
        var board = new Board("7k/8/8/8/8/8/8/3K3R w K - 0 1");

        Assert.DoesNotContain(GenerateKingMoves(board), move => move.IsCastling());
    }

    private static List<Move> GenerateKingMoves(Board board, bool isInCheck = false)
    {
        MethodInfo method = typeof(MoveGenerator).GetMethod(
            "GenerateKingMoves",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateKingMoves method was not found.");

        return (List<Move>)method.Invoke(null, [board, isInCheck])!;
    }

    private static MoveResult ToResult(Move move) => new(move.From(), move.To(), move.Flags());

    private static MoveResult Move(string from, string to) => new(Square(from), Square(to), 0);

    private static int Square(string square) => StringRepr.SquareConversion(square);

    private readonly record struct MoveResult(int From, int To, int Flags);
}
