using Core.DataStructures;
using Core.Helpers;
using Engine;
using Xunit;

namespace ChessEngine.Tests.Engine;

public class BotApplyMoveTests
{
    [Fact]
    public void ApplyMoveUci_UpdatesCurrentPosition()
    {
        var bot = new Bot();

        bot.ApplyMove("e2e4");

        Assert.Contains(
            "Fen         : rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            bot.GetBoardDiagram());
    }

    [Fact]
    public void ApplyMove_UpdatesCurrentPosition()
    {
        var bot = new Bot();

        bot.ApplyMove(new Move(
            StringRepr.SquareConversion("e2"),
            StringRepr.SquareConversion("e4"),
            Move.GenerateSpecialMoveFlag(isDoublePawnPush: true)));

        Assert.Contains(
            "Fen         : rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
            bot.GetBoardDiagram());
    }

    [Fact]
    public void ApplyMoveUci_RejectsIllegalMove()
    {
        var bot = new Bot();

        Assert.Throws<ArgumentException>(() => bot.ApplyMove("e2e5"));
    }
}
