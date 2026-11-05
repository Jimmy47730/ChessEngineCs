using Core.DataStructures;
using Core.Helpers;
using Engine.MoveGeneration;
using Xunit;

namespace ChessEngine.Tests.Engine.MoveGeneration;

public class PerftTests
{
    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 1, 20)]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 2, 400)]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 3, 8902)]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 4, 197281)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 1, 48)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 2, 2039)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 3, 97862)]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 4, 4085603)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 1, 14)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 2, 191)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 3, 2812)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 4, 43238)]
    [InlineData("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1", 1, 6)]
    [InlineData("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1", 2, 264)]
    [InlineData("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1", 3, 9467)]
    [InlineData("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1", 4, 422333)]
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 1, 44)]
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 2, 1486)]
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62379)]
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4, 2103487)]
    public void CountPositions_MatchesExpectedPerft(string fen, int depth, long expectedPositions)
    {
        var board = new Board(fen);
        var divide = DividePositions(board, depth);
        long actualPositions = divide.Sum(entry => entry.Count);
        string breakdown = string.Join(
            Environment.NewLine,
            divide.Select(entry => $"{StringRepr.MoveToUci(entry.Move).TrimEnd('\0')}: {entry.Count}")
        );

        Assert.True(
            actualPositions == expectedPositions,
            $"Expected {expectedPositions} positions, got {actualPositions} at depth {depth}.{Environment.NewLine}" +
            $"Perft divide:{Environment.NewLine}{breakdown}"
        );
    }

    [Fact]
    public void GenerateMoves_RejectsEnPassantThatExposesKingToRook()
    {
        var board = new Board("4r2k/8/8/3pP3/8/8/8/4K3 w - d6 0 1");

        Assert.DoesNotContain(
            MoveGenerator.GenerateMoves(board),
            move => move.From() == 36 && move.To() == 43 && move.IsEnPassant()
        );
    }

    [Fact]
    public void GenerateMoves_IncludesLegalEnPassant()
    {
        var board = new Board("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");

        Assert.Contains(
            MoveGenerator.GenerateMoves(board),
            move => move.From() == 36 && move.To() == 43 && move.IsEnPassant()
        );
    }

    private static long CountPositions(Board board, int depth)
    {
        if (depth == 0)
            return 1;

        long positions = 0;
        foreach (Move move in MoveGenerator.GenerateMoves(board))
        {
            UndoInfo undoInfo = board.ApplyMove(move);
            try
            {
                positions += CountPositions(board, depth - 1);
            }
            finally
            {
                board.UndoMove(move, undoInfo);
            }
        }

        return positions;
    }

    private static List<(Move Move, long Count)> DividePositions(Board board, int depth)
    {
        if (depth <= 0)
            throw new ArgumentOutOfRangeException(nameof(depth), "Perft divide requires a depth of at least 1.");

        List<(Move Move, long Count)> divide = [];
        foreach (Move move in MoveGenerator.GenerateMoves(board))
        {
            UndoInfo undoInfo = board.ApplyMove(move);
            long positions;
            try
            {
                positions = CountPositions(board, depth - 1);
            }
            finally
            {
                board.UndoMove(move, undoInfo);
            }

            divide.Add((move, positions));
        }

        return divide;
    }
}
