using Core.Board;
using Core.Helpers;
using Engine.MoveGeneration;

namespace ChessEngine.Tests;

public class CheckLegalityTests
{
    private static HashSet<string> GetLegalMoves(string fen)
    {
        var board = new BoardState(fen);
        return [.. MoveGenerator.Instance()
            .GenerateMoves(board)
            .Select(move => move.ToUci().Trim())];
    }

    [Fact]
    public void SingleRookCheck_CanBlock()
    {
        // Rook on e5 checking king on e1, bishop on c1, knight on g3 and rook on b5
        var moves = GetLegalMoves("4r1k1/8/8/1R6/8/6N1/8/2B1K3 w - - 0 1");

        // Bishop to e3, blocking check
        Assert.Contains("c1e3", moves);
        Assert.DoesNotContain("c1b2", moves);

        // Rook to e5, blocking check
        Assert.Contains("b5e5", moves);
        Assert.DoesNotContain("b5b6", moves);

        // Knight to e2/e4, blocking check
        Assert.Contains("g3e2", moves);
        Assert.Contains("g3e4", moves);
        Assert.DoesNotContain("g3f5", moves);

    }

    [Fact]
    public void SingleBishopCheck_CanBlock()
    {
        // Bishop on b7 checking king on g2, bishop on e8, knight on c3 and rook on f1
        var moves = GetLegalMoves("4B2k/1b6/8/8/8/2N5/6K1/5R2 w - - 0 1");

        // Bishop to c6, blocking check
        Assert.Contains("e8c6", moves);
        Assert.DoesNotContain("e8d7", moves);

        // Knight to d5/e4, blocking check
        Assert.Contains("c3d5", moves);
        Assert.Contains("c3e4", moves);
        Assert.DoesNotContain("c3b5", moves);

        // Rook to f3, blocking check
        Assert.Contains("f1f3", moves);
        Assert.DoesNotContain("f1f2", moves);

    }

    [Fact]
    public void King_CannotMoveIntoCheckFromRookOrBishop()
    {
        var rookCheckMoves = GetLegalMoves("4r1k1/8/8/8/8/8/4K3/8 w - - 0 1");
        var bishopCheckMoves = GetLegalMoves("7k/1b6/8/8/8/8/6K1/8 w - - 0 1");

        Assert.DoesNotContain("e2e3", rookCheckMoves);
        //Assert.DoesNotContain("e2e1", rookCheckMoves);
        Assert.DoesNotContain("g2f3", bishopCheckMoves);
        Assert.DoesNotContain("g2h1", bishopCheckMoves);
    }

    [Fact]
    public void KnightCheck_AllowsCapturingTheCheckingPiece()
    {
        var moves = GetLegalMoves("7k/8/8/8/8/5n2/6P1/R3K3 w - - 0 1");

        Assert.Contains("g2f3", moves);
        Assert.DoesNotContain("a1a2", moves);
    }

    [Fact]
    public void PawnCheck_AllowsCapturingTheCheckingPiece()
    {
        var moves = GetLegalMoves("7k/8/8/8/8/8/R2p4/4K3 w - - 0 1");

        Assert.Contains("a2d2", moves);
    }

    [Fact]
    public void DoubleCheck_AllowsOnlyKingMoves()
    {
        var board = new BoardState("4r2k/8/8/8/1b6/8/8/R3K3 w - - 0 1");
        var moves = MoveGenerator.Instance().GenerateMoves(board);

        Assert.NotEmpty(moves);
        Assert.All(moves, move => Assert.Equal(PieceType.King, move.MovedPiece));
    }

    [Fact]
    public void FilePin_RestrictsRookToThePinFile()
    {
        var moves = GetLegalMoves("4r2k/8/8/8/8/8/4R3/4K3 w - - 0 1");
        var rookMoves = moves.Where(move => move.StartsWith("e2", StringComparison.Ordinal));

        Assert.NotEmpty(rookMoves);
        Assert.All(rookMoves, move => Assert.Equal('e', move[2]));
    }

    [Fact]
    public void DiagonalPin_PreventsKnightFromMoving()
    {
        var moves = GetLegalMoves("7k/8/8/8/1b6/8/3N4/4K3 w - - 0 1");

        Assert.DoesNotContain(moves, move => move.StartsWith("d2", StringComparison.Ordinal));
    }

    [Fact]
    public void DiagonallyPinnedPawn_CanCapturePinnerButNotMoveOffPinRay()
    {
        var moves = GetLegalMoves("7k/8/8/8/8/2b1n3/3P4/4K3 w - - 0 1");

        Assert.Contains("d2c3", moves);
        Assert.DoesNotContain("d2e3", moves);
    }

    [Fact]
    public void King_CannotMoveOntoASquareAttackedByARook()
    {
        var moves = GetLegalMoves("5r1k/8/8/8/8/8/8/4K3 w - - 0 1");

        Assert.DoesNotContain("e1f1", moves);
    }
}
