using Core.DataStructures;
using Core.Helpers;
using Engine.Evaluation;
using Engine.MoveGeneration;
using Engine.Search;
using Xunit;

namespace ChessEngine.Tests.Engine;

public class SearcherTests
{
    [Fact]
    public void Search_ExtendsCaptureSequenceAtDepthLimitAndRestoresBoard()
    {
        var board = new Board("7k/8/4p3/3q4/2P5/8/3R4/K7 w - - 0 1");
        var drawManager = new DrawManager();
        drawManager.ResetPositions(board.zobristHash);
        var searcher = new Searcher(board, drawManager);
        string originalFen = StringRepr.ToFen(board);

        Searcher.SearchResult result = searcher.Search(depthTarget: 1);

        Assert.True(result.BestLine.Count >= 3);
        Assert.All(result.BestLine.Take(3), move => Assert.True(move.IsCapture()));
        Assert.Equal(originalFen, StringRepr.ToFen(board));
        Assert.Equal(board.zobristHash, drawManager.CurrentPositionHash);
    }

    [Fact]
    public void Search_StopsCaptureExtensionWhenNoCaptureIsAvailable()
    {
        var board = new Board();
        var drawManager = new DrawManager();
        drawManager.ResetPositions(board.zobristHash);
        var searcher = new Searcher(board, drawManager);

        Searcher.SearchResult result = searcher.Search(depthTarget: 1);

        Assert.Single(result.BestLine);
    }

    [Fact]
    public void Search_ExtendsWhenSideToMoveHasFiveOrFewerLegalMoves()
    {
        var board = new Board("7k/8/8/8/8/8/8/K7 w - - 0 1");
        var drawManager = new DrawManager();
        drawManager.ResetPositions(board.zobristHash);
        var searcher = new Searcher(board, drawManager);

        Assert.InRange(MoveGenerator.GenerateMoves(board).Count, 1, 5);

        Searcher.SearchResult result = searcher.Search(depthTarget: 1);

        Assert.True(result.BestLine.Count > 1);
        Assert.True(result.BestLine.Count <= 11);
    }

    [Fact]
    public void Search_RecognizesMateInOneAsFastestPossibleMate()
    {
        var board = new Board("7k/8/5KQ1/8/8/8/8/8 w - - 0 1");
        var drawManager = new DrawManager();
        drawManager.ResetPositions(board.zobristHash);
        var searcher = new Searcher(board, drawManager);

        Searcher.SearchResult result = searcher.Search(depthTarget: 1);

        Assert.Equal(999_999, result.Evaluation);
        Assert.Single(result.BestLine);
        Assert.Equal("g6g7", StringRepr.MoveToUci(result.BestMove));
    }
}
