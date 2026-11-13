using Chess;
using Core.Helpers;
using Core.DataStructures;
using Engine.Evaluation;
using Engine.MoveGeneration;
using Engine.Search;
using System.ComponentModel.DataAnnotations;

namespace Engine;

public class Bot
{
    public bool IsThinking { get; private set; }
    public Board currentBoard = new();
    public Action<string>? OnMoveChosen;
    private readonly DrawManager drawManager;
    public readonly Searcher searcher;
    private readonly object thinkingLock = new();
    private CancellationTokenSource? searchCancellation;
    private const int thinkTimeOverride = 3000; // leave 0 to disable

    public Bot()
    {
        drawManager = new();
        searcher = new(currentBoard, drawManager);
        drawManager.ResetPositions(currentBoard.zobristHash);
    }

    // Bot implementation

    public void NotifyNewGame()
    {
        currentBoard = new Board();
        drawManager.ResetPositions(currentBoard.zobristHash);
    }

    public void SetPosition(string fen = "")
    {
        if (string.IsNullOrEmpty(fen))
            currentBoard = new Board();
        else
            currentBoard = new Board(fen);
        drawManager.ResetPositions(currentBoard.zobristHash);
    }

    public string GetBoardDiagram()
    {
        return StringRepr.BoardDiagram(currentBoard, includeFen: true, includeZobristKey: true);
    }

    public void Quit()
    {
        StopThinking();
    }

    public void ApplyMove(Move move)
    {
        EnsureDrawHistoryMatchesBoard();
        List<Move> legalMoves = MoveGenerator.GenerateMoves(currentBoard);
        foreach (Move legalMove in legalMoves)
        {
            if (legalMove.From() == move.From() &&
                legalMove.To() == move.To() &&
                legalMove.Flags() == move.Flags())
            {
                ApplyTrackedMove(legalMove);
                return;
            }
        }

        throw new ArgumentException("The move is not legal in the current position.", nameof(move));
    }

    public void ApplyMove(string moveUci)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moveUci);

        EnsureDrawHistoryMatchesBoard();
        List<Move> legalMoves = MoveGenerator.GenerateMoves(currentBoard);
        foreach (Move legalMove in legalMoves)
        {
            if (string.Equals(StringRepr.MoveToUci(legalMove), moveUci, StringComparison.OrdinalIgnoreCase))
            {
                ApplyTrackedMove(legalMove);
                return;
            }
        }

        throw new ArgumentException($"The move '{moveUci}' is not legal in the current position.", nameof(moveUci));
    }

    public void StopThinking()
    {
        lock (thinkingLock)
            searchCancellation?.Cancel();
    }

    // Time is always treated in milliseconds
    public static int DefineThinkingTime(int whiteTime, int blackTime, int whiteInc, int blackInc)
    {
        // Temporary implementation
        return whiteTime / 20 + whiteInc;
    }

    public Task ThinkAsync(int time)
    {
        CancellationToken cancellationToken = BeginThinking(time);
        return Task.Run(() =>
        {
            try
            {
                RunSearch(cancellationToken);
            }
            finally
            {
                EndThinking();
            }
        });
    }

    private void RunSearch(CancellationToken cancellationToken)
    {
        EnsureDrawHistoryMatchesBoard();
        var searcher = new Searcher(currentBoard, drawManager);
        Searcher.SearchResult searchResult = searcher.Search(cancellationToken: cancellationToken);
        if (!searchResult.BestMove.IsValid())
        {
            Logger.Warn("No legal moves available.");
            OnMoveChosen?.Invoke("0000");
            return;
        }

        OnSearchComplete(searchResult);
    }

    private CancellationToken BeginThinking(int time)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(time);

        lock (thinkingLock)
        {
            if (IsThinking)
                throw new InvalidOperationException("The bot is already thinking.");

            searchCancellation = new CancellationTokenSource();
            if (thinkTimeOverride > 0)
                time = Math.Min(time, thinkTimeOverride);
            searchCancellation.CancelAfter(time);
            IsThinking = true;
            return searchCancellation.Token;
        }
    }

    private void EndThinking()
    {
        lock (thinkingLock)
        {
            searchCancellation = null;
            IsThinking = false;
        }
    }

    private void OnSearchComplete(Searcher.SearchResult searchResult)
    {
        string bestMoveUci = StringRepr.MoveToUci(searchResult.BestMove);
        string bestLine = string.Join(" ", searchResult.BestLine.Select(StringRepr.MoveToUci));
        Logger.Info($"Chosen move: {searchResult.BestMove} ({bestMoveUci})");
        Logger.Info($"Best line found: {bestLine}");
        Logger.Info($"Current evaluation: {searchResult.Evaluation}");
        OnMoveChosen?.Invoke(bestMoveUci);
    }

    private void EnsureDrawHistoryMatchesBoard()
    {
        if (drawManager.CurrentPositionHash != currentBoard.zobristHash)
            drawManager.ResetPositions(currentBoard.zobristHash);
    }

    private UndoInfo ApplyTrackedMove(Move move)
    {
        char movedPiece = currentBoard.GetPieceAt(move.From());
        bool[] previousCastlingRights = currentBoard.GetCastlingRights();
        UndoInfo undoInfo = currentBoard.ApplyMove(move);
        bool[] currentCastlingRights = currentBoard.GetCastlingRights();
        bool lostCastlingRights = false;
        for (int i = 0; i < previousCastlingRights.Length; i++)
        {
            if (previousCastlingRights[i] && !currentCastlingRights[i])
            {
                lostCastlingRights = true;
                break;
            }
        }

        bool isIrreversible = char.ToUpperInvariant(movedPiece) == 'P' ||
            move.IsCapture() ||
            lostCastlingRights;
        drawManager.PushPosition(currentBoard.zobristHash, isIrreversible);
        return undoInfo;
    }
}
