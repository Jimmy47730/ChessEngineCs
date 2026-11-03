using Core.DataStructures;
using Core.Helpers;
using Xunit;

namespace ChessEngine.Tests.Core.DataStructures;

public class BoardApplyMoveTests
{
    [Fact]
    public void ApplyMove_UpdatesPawnPushAndPositionMetadata()
    {
        var board = new Board();

        board.ApplyMove(new Move(Square("e2"), Square("e4"), Move.GenerateSpecialMoveFlag(true)));

        Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", StringRepr.ToFen(board));
        Assert.Equal(board.zobristHash, new Board(StringRepr.ToFen(board)).zobristHash);
        Assert.Equal(0UL, board.GetBitboard(PieceType.Pawn, SideColor.White) & Bit("e2"));
        Assert.NotEqual(0UL, board.GetBitboard(PieceType.Pawn, SideColor.White) & Bit("e4"));
    }

    [Fact]
    public void ApplyMove_RemovesCapturedPieceFromItsBitboard()
    {
        var board = new Board("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1");

        board.ApplyMove(new Move(
            Square("e4"),
            Square("d5"),
            Move.GenerateSpecialMoveFlag(CaptureType.Normal)));

        Assert.Equal("4k3/8/8/3P4/8/8/8/4K3 b - - 0 1", StringRepr.ToFen(board));
        Assert.Equal(0UL, board.GetBitboard(PieceType.Pawn, SideColor.Black));
    }

    [Fact]
    public void ApplyMove_PromotesPawnToSelectedPiece()
    {
        var board = new Board("4k3/P7/8/8/8/8/8/4K3 w - - 0 1");

        board.ApplyMove(new Move(
            Square("a7"),
            Square("a8"),
            Move.GeneratePromotionFlag(PieceType.Queen, isCapture: false)));

        Assert.Equal("Q3k3/8/8/8/8/8/8/4K3 b - - 0 1", StringRepr.ToFen(board));
        Assert.Equal(0UL, board.GetBitboard(PieceType.Pawn, SideColor.White));
        Assert.NotEqual(0UL, board.GetBitboard(PieceType.Queen, SideColor.White) & Bit("a8"));
    }

    [Fact]
    public void ApplyMove_PromotesWhileCapturing()
    {
        var board = new Board("1r2k3/P7/8/8/8/8/8/4K3 w - - 0 1");

        board.ApplyMove(new Move(
            Square("a7"),
            Square("b8"),
            Move.GeneratePromotionFlag(PieceType.Queen, isCapture: true)));

        Assert.Equal("1Q2k3/8/8/8/8/8/8/4K3 b - - 0 1", StringRepr.ToFen(board));
        Assert.Equal(0UL, board.GetBitboard(PieceType.Rook, SideColor.Black));
        Assert.NotEqual(0UL, board.GetBitboard(PieceType.Queen, SideColor.White) & Bit("b8"));
    }

    [Fact]
    public void ApplyMove_HandlesEnPassantCapture()
    {
        var board = new Board("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");

        board.ApplyMove(new Move(
            Square("e5"),
            Square("d6"),
            Move.GenerateSpecialMoveFlag(CaptureType.EnPassant)));

        Assert.Equal("4k3/8/3P4/8/8/8/8/4K3 b - - 0 1", StringRepr.ToFen(board));
        Assert.Equal(0UL, board.GetBitboard(PieceType.Pawn, SideColor.Black));
    }

    [Fact]
    public void ApplyMove_CastlesKingAndRookAndClearsRights()
    {
        var board = new Board("4k3/8/8/8/8/8/8/4K2R w K - 0 1");

        board.ApplyMove(new Move(
            Square("e1"),
            Square("g1"),
            Move.GenerateSpecialMoveFlag(CastlingType.KingSide)));

        Assert.Equal("4k3/8/8/8/8/8/8/5RK1 b - - 1 1", StringRepr.ToFen(board));
        Assert.Equal(0UL, board.GetBitboard(PieceType.Rook, SideColor.White) & Bit("h1"));
        Assert.NotEqual(0UL, board.GetBitboard(PieceType.Rook, SideColor.White) & Bit("f1"));
    }

    private static int Square(string name) => StringRepr.SquareConversion(name);
    private static ulong Bit(string name) => Operations.SquareToU64(Square(name));
}
