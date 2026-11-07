using Engine.Evaluation;
using Xunit;

namespace ChessEngine.Tests.Engine;

public class DrawManagerTests
{
    [Fact]
    public void PopPosition_RestoresHistoryAcrossIrreversibleMove()
    {
        var drawManager = new DrawManager();
        drawManager.ResetPositions(1);
        drawManager.PushPosition(2, isIrreversible: false);
        drawManager.PushPosition(1, isIrreversible: false);
        drawManager.PushPosition(2, isIrreversible: false);
        drawManager.PushPosition(1, isIrreversible: false);

        Assert.True(drawManager.IsThreefoldRepetition());

        drawManager.PushPosition(3, isIrreversible: true);
        drawManager.PushPosition(1, isIrreversible: false);

        Assert.False(drawManager.IsThreefoldRepetition());

        drawManager.PopPosition();
        drawManager.PopPosition();

        Assert.True(drawManager.IsThreefoldRepetition());
    }

    [Fact]
    public void IsThreefoldRepetition_DoesNotCountPositionsBeforeIrreversibleMove()
    {
        var drawManager = new DrawManager();
        drawManager.ResetPositions(1);
        drawManager.PushPosition(2, isIrreversible: true);
        drawManager.PushPosition(1, isIrreversible: false);

        Assert.False(drawManager.IsThreefoldRepetition());
    }
}
