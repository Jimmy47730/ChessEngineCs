using Core.DataStructures;
using Core.Helpers;
using Engine;
using Engine.Search;
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
    public async Task StopThinking_CancelsSearchAndRestoresPosition()
    {
        var bot = new Bot();
        string originalPosition = bot.GetBoardDiagram();
        string? selectedMove = null;
        bot.OnMoveChosen = move => selectedMove = move;

        Task search = bot.ThinkAsync(int.MaxValue);
        Assert.True(bot.IsThinking);
        bot.StopThinking();
        await search;

        Assert.NotNull(selectedMove);
        Assert.Contains(
            MoveGenerator.GenerateMoves(new Board()),
            move => StringRepr.MoveToUci(move) == selectedMove);
        Assert.Equal(originalPosition, bot.GetBoardDiagram());
        Assert.False(bot.IsThinking);
    }

    [Fact]
    public async Task ThinkAsync_StopsSearchWhenTimeLimitExpires()
    {
        var bot = new Bot();
        string? selectedMove = null;
        bot.OnMoveChosen = move => selectedMove = move;

        await bot.ThinkAsync(0);

        Assert.NotNull(selectedMove);
        Assert.False(bot.IsThinking);
    }

    [Fact]
    public void Search_CancelledBeforeFirstRootMoveReturnsEvaluatedLegalFallback()
    {
        var bot = new Bot();
        string originalPosition = bot.GetBoardDiagram();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Searcher.SearchResult result = bot.searcher.Search(cancellationToken: cancellation.Token);

        Assert.True(result.Cancelled);
        Assert.Contains(
            MoveGenerator.GenerateMoves(bot.currentBoard),
            move => move.From() == result.BestMove.From() &&
                move.To() == result.BestMove.To() &&
                move.Flags() == result.BestMove.Flags());
        Assert.True(Math.Abs(result.Evaluation) < 1_100_000);
        Assert.Equal(result.BestMove, Assert.Single(result.BestLine));
        Assert.Equal(originalPosition, bot.GetBoardDiagram());
    }
}
