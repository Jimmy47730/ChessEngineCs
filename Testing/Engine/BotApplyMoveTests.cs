using Core.DataStructures;
using Core.Helpers;
using Engine;
using Engine.MoveGeneration;
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

    [Fact]
    public void Think_SelectsLegalMoveAndRestoresPosition()
    {
        var bot = new Bot();
        string originalPosition = bot.GetBoardDiagram();
        string? selectedMove = null;
        bot.OnMoveChosen = move => selectedMove = move;

        bot.Think(0);

        Assert.NotNull(selectedMove);
        Assert.Contains(
            MoveGenerator.GenerateMoves(new Board()),
            move => StringRepr.MoveToUci(move) == selectedMove);
        Assert.Equal(originalPosition, bot.GetBoardDiagram());
        Assert.False(bot.IsThinking);
    }
}
