using System.Reflection;
using System.Runtime.CompilerServices;
using Chess;
using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class MoveGeneratorPawnMovesTests
{
    [Theory]
    [InlineData("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1", "e2", "e3", "e4")]
    [InlineData("4k3/4p3/8/8/4P3/8/8/4K3 b - - 0 1", "e7", "e6", "e5")]
    public void GeneratePawnMoves_GeneratesSingleAndDoublePushes(string fen, string from, string singleTo, string doubleTo)
    {
        var moves = GeneratePawnMoves(new Board(fen));

        PrintMoves(moves);

        Assert.Contains(moves, move => IsMove(move, from, singleTo, 0));
        Assert.Contains(moves, move => IsMove(move, from, doubleTo, Move.GenerateSpecialMoveFlag(true)));
        Assert.Equal(2, moves.Count);
    }

    [Theory]
    [InlineData("4k3/8/8/8/8/4n3/4P3/4K3 w - - 0 1", "e2")]
    [InlineData("4k3/4p3/4P3/8/8/8/8/4K3 b - - 0 1", "e7")]
    public void GeneratePawnMoves_DoesNotPushWhenTheSingleStepIsBlocked(string fen, string from)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move => move.From() == Square(from));
    }

    [Theory]
    [InlineData("4k3/8/8/8/4n3/8/4P3/4K3 w - - 0 1", "e2", "e3", "e4")]
    [InlineData("4k3/4p3/8/4N3/8/8/8/4K3 b - - 0 1", "e7", "e6", "e5")]
    public void GeneratePawnMoves_DoesNotDoublePushWhenTheDestinationIsBlocked(
        string fen,
        string from,
        string singleDestination,
        string doubleDestination)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);

        Assert.Contains(moves, move => move.From() == Square(from) && move.To() == Square(singleDestination));
        Assert.DoesNotContain(moves, move => move.From() == Square(from) && move.To() == Square(doubleDestination));
    }

    [Theory]
    [InlineData("4k3/8/8/3p1p2/4P3/8/8/4K3 w - - 0 1", "e4", "d5", "f5")]
    [InlineData("4k3/8/8/8/4p3/3P1P2/8/4K3 b - - 0 1", "e4", "d3", "f3")]
    public void GeneratePawnMoves_GeneratesCapturesOnBothDiagonals(string fen, string from, string left, string right)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);

        Assert.Contains(moves, move => IsMove(move, from, left, Move.GenerateSpecialMoveFlag(CaptureType.Normal)));
        Assert.Contains(moves, move => IsMove(move, from, right, Move.GenerateSpecialMoveFlag(CaptureType.Normal)));
    }

    [Theory]
    [InlineData("4k3/8/8/3P1P2/4P3/8/8/4K3 w - - 0 1", "e4", "d5", "f5")]
    [InlineData("4k3/8/8/8/4p3/3p1p2/4p3/4K3 b - - 0 1", "e4", "d3", "f3")]
    public void GeneratePawnMoves_DoesNotCaptureFriendlyPawns(string fen, string from, string left, string right)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move => move.From() == Square(from) &&
            (move.To() == Square(left) || move.To() == Square(right)));
    }

    [Theory]
    [InlineData("4k3/8/8/P6p/8/8/8/4K3 w - - 0 1", "a5", "h5")]
    [InlineData("4k3/8/p7/7P/8/8/8/4K3 w - - 0 1", "h5", "a6")]
    [InlineData("4k3/8/8/8/7p/P7/8/4K3 b - - 0 1", "h4", "a3")]
    [InlineData("4k3/8/8/8/p7/8/7P/4K3 b - - 0 1", "a4", "h2")]
    public void GeneratePawnMoves_DoesNotWrapCapturesAcrossBoardEdges(string fen, string from, string wrappedTo)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move => move.From() == Square(from) && move.To() == Square(wrappedTo));
    }

    [Fact]
    public void GeneratePawnMoves_RespectsAllowedDestinationsForPushesAndCaptures()
    {
        var board = new Board("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1");

        var moves = GeneratePawnMoves(board, SquareBit("d5"));
        PrintMoves(moves);

        Assert.Single(moves);
        Assert.Contains(moves, move => IsMove(move, "e4", "d5", Move.GenerateSpecialMoveFlag(CaptureType.Normal)));
    }

    [Fact]
    public void GeneratePawnMoves_RestrictsDoublePushByItsDestination()
    {
        var board = new Board("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1");

        var moves = GeneratePawnMoves(board, SquareBit("e4"));
        PrintMoves(moves);

        Assert.Single(moves);
        Assert.Contains(moves, move => IsMove(move, "e2", "e4", Move.GenerateSpecialMoveFlag(true)));
    }

    [Fact]
    public void GeneratePawnMoves_AllowsPinnedPawnToMoveAlongItsPinRay()
    {
        var board = new Board("4r1k1/8/8/8/8/8/4P3/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("e8"));

        var moves = GeneratePawnMoves(board, pinRays: [pinRay]);
        PrintMoves(moves);

        Assert.Contains(moves, move => IsMove(move, "e2", "e3", 0));
        Assert.Contains(moves, move => IsMove(move, "e2", "e4", Move.GenerateSpecialMoveFlag(true)));
        Assert.Equal(2, moves.Count);
    }

    [Theory]
    [InlineData("4k3/8/8/8/8/2b5/3P4/4K3 w - - 0 1", "e1", "c3", "d2", "d3")]
    [InlineData("4r1k1/8/8/8/8/3p4/4P3/4K3 w - - 0 1", "e1", "e8", "e2", "d3")]
    [InlineData("7k/8/8/8/K2P3r/8/8/8 w - - 0 1", "a4", "h4", "d4", "d5")]
    public void GeneratePawnMoves_DoesNotAllowPinnedPawnToLeaveItsPinRay(
        string fen,
        string kingSquare,
        string attackerSquare,
        string from,
        string offRayDestination)
    {
        var board = new Board(fen);
        ulong pinRay = Operations.GetRay(SquareBit(kingSquare), SquareBit(attackerSquare));

        var moves = GeneratePawnMoves(board, pinRays: [pinRay]);
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move => move.From() == Square(from) && move.To() == Square(offRayDestination));
    }

    [Fact]
    public void GeneratePawnMoves_AllowsPinnedPawnToCaptureAlongItsPinRay()
    {
        var board = new Board("4k3/8/8/8/8/2b5/3P4/4K3 w - - 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("c3"));

        var moves = GeneratePawnMoves(board, pinRays: [pinRay], allowedDestinations: SquareBit("c3"));
        PrintMoves(moves);

        Assert.Single(moves);
        Assert.Contains(moves, move => IsMove(move, "d2", "c3", Move.GenerateSpecialMoveFlag(CaptureType.Normal)));
    }

    [Theory]
    [InlineData("1r5k/P7/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/1p6/2R4K b - - 0 1")]
    public void GeneratePawnMoves_GeneratesAllQuietAndCapturePromotions(string fen)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);
        
        Assert.Equal(8, moves.Count);
    }

    [Theory]
    [InlineData("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5", "d6")]
    [InlineData("4k3/8/8/4Pp2/8/8/8/4K3 w - f6 0 1", "e5", "f6")]
    [InlineData("4k3/8/8/8/3Pp3/8/8/4K3 b - d3 0 1", "e4", "d3")]
    [InlineData("4k3/8/8/8/4pP2/8/8/4K3 b - f3 0 1", "e4", "f3")]
    public void GeneratePawnMoves_GeneratesEnPassantCapturesOnBothDiagonals(string fen, string from, string to)
    {
        var moves = GeneratePawnMoves(new Board(fen));
        PrintMoves(moves);

        Assert.Contains(moves, move => IsMove(move, from, to, Move.GenerateSpecialMoveFlag(CaptureType.EnPassant)));
    }

    [Fact]
    public void GeneratePawnMoves_DoesNotGenerateEnPassantForNonDiagonalOrMissingCapture()
    {
        var nonDiagonalTarget = new Board("4k3/8/4p3/4P3/8/8/8/4K3 w - e6 0 1");
        var missingCapturedPawn = new Board("4k3/8/8/4P3/8/8/8/4K3 w - d6 0 1");

        var nonDiagonalMoves = GeneratePawnMoves(nonDiagonalTarget);
        PrintMoves(nonDiagonalMoves);
        var missingCapturedPawnMoves = GeneratePawnMoves(missingCapturedPawn);
        PrintMoves(missingCapturedPawnMoves);

        Assert.DoesNotContain(nonDiagonalMoves, move => move.IsEnPassant());
        Assert.DoesNotContain(missingCapturedPawnMoves, move => move.IsEnPassant());
    }

    [Fact]
    public void GeneratePawnMoves_DoesNotAllowEnPassantToExposeKingToHorizontalRookAttack()
    {
        var board = new Board("7k/8/8/4KPpr/8/8/8/8 w - g6 0 1");
        var moves = GeneratePawnMoves(board);
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move => move.IsEnPassant());
    }

    [Fact]
    public void GeneratePawnMoves_DoesNotAllowEnPassantToExposeKingOnVerticalPin()
    {
        var board = new Board("4r2k/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("e1"), SquareBit("e8"));

        var moves = GeneratePawnMoves(board, pinRays: [pinRay]);
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move =>
            IsMove(move, "e5", "d6", Move.GenerateSpecialMoveFlag(CaptureType.EnPassant)));
    }

    [Fact]
    public void GeneratePawnMoves_AllowsEnPassantAlongDiagonalPin()
    {
        var board = new Board("7k/5b2/8/3Pp3/2K5/8/8/8 w - e6 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("c4"), SquareBit("f7"));

        var moves = GeneratePawnMoves(board, pinRays: [pinRay]);
        PrintMoves(moves);

        Assert.Contains(moves, move =>
            IsMove(move, "d5", "e6", Move.GenerateSpecialMoveFlag(CaptureType.EnPassant)));
    }

    [Fact]
    public void GeneratePawnMoves_DoesNotAllowEnPassantOffDiagonalPin()
    {
        var board = new Board("7k/5b2/8/2pP4/2K5/8/8/8 w - c6 0 1");
        ulong pinRay = Operations.GetRay(SquareBit("c4"), SquareBit("f7"));

        var moves = GeneratePawnMoves(board, pinRays: [pinRay]);
        PrintMoves(moves);

        Assert.DoesNotContain(moves, move =>
            IsMove(move, "d5", "c6", Move.GenerateSpecialMoveFlag(CaptureType.EnPassant)));
    }

    private static List<Move> GeneratePawnMoves(
        Board board,
        ulong allowedDestinations = ulong.MaxValue,
        List<ulong>? pinRays = null)
    {
        MethodInfo method = typeof(MoveGenerator).GetMethod(
            "GeneratePawnMoves",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GeneratePawnMoves method was not found.");

        return (List<Move>)method.Invoke(null, [board, allowedDestinations, pinRays ?? []])!;
    }

    private static bool IsMove(Move move, string from, string to, int flags) =>
        move.From() == Square(from) && move.To() == Square(to) && move.Flags() == flags;

    private static ulong SquareBit(string square) => Operations.SquareToU64(Square(square));

    private static int Square(string square) => StringRepr.SquareConversion(square);

    private static void PrintMoves(List<Move> moves, [CallerMemberName] string testName = "")
    {
        foreach (Move move in moves)
        {
            Logger.Debug($"{testName}: {StringRepr.MoveToUci(move).TrimEnd('\0')}");
        }

        Logger.Debug($"{testName}: {new string('-', 40)}");
    }
}
