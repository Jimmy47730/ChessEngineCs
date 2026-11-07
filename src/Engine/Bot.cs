using Chess;
using Core.Helpers;
using Core.DataStructures;
using Engine.Evaluation;
using Engine.MoveGeneration;
namespace Engine;

public class Bot
{
    private const int MateScore = 1_000_000;
    private const int SearchInfinity = 1_100_000;
    private const int SearchDepth = 4;

    public bool IsThinking { get; private set; }
    public Board currentBoard = new();
    public Action<string>? OnMoveChosen;
    private readonly DrawManager drawManager = new();

    public Bot()
    {
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    // Time is always treated in milliseconds
    public static int DefineThinkingTime(int whiteTime, int blackTime, int whiteInc, int blackInc)
    {
        // Temporary implementation
        return whiteTime / 20 + whiteInc;
    }

    public void Think(int time)
    {
        EnsureDrawHistoryMatchesBoard();
        IsThinking = true;
        try
        {
            SearchResult searchResult = Search(SearchDepth);
            if (!searchResult.BestMove.IsValid())
            {
                Logger.Warn("No legal moves available.");
                OnMoveChosen?.Invoke("0000");
                return;
            }

            OnSearchComplete(searchResult);
        }
        finally
        {
            IsThinking = false;
        }
    }

    private void OnSearchComplete(SearchResult searchResult)
    {
        string bestMoveUci = StringRepr.MoveToUci(searchResult.BestMove);
        string bestLine = string.Join(" ", searchResult.BestLine.Select(StringRepr.MoveToUci));
        Logger.Info($"Chosen move: {searchResult.BestMove} ({bestMoveUci})");
        Logger.Info($"Best line found: {bestLine}");
        Logger.Info($"Current evaluation: {searchResult.Evaluation}");
        OnMoveChosen?.Invoke(bestMoveUci);
    }

    private SearchResult Search(int depthTarget)
    {
        if (depthTarget < 1)
            throw new ArgumentOutOfRangeException(nameof(depthTarget), "Search depth must be at least one.");

        List<Move> legalMoves = MoveGenerator.GenerateMoves(currentBoard);
        Logger.Debug($"Generated {legalMoves.Count} legal moves.");
        if (legalMoves.Count == 0)
            return new SearchResult(Move.Null(), 0, []);

        Move bestMove = legalMoves[0];
        int bestScore = -SearchInfinity;
        List<Move> bestLine = [];
        int alpha = -SearchInfinity;

        foreach (Move move in legalMoves)
        {
            UndoInfo undoInfo = ApplyTrackedMove(move);
            SearchResult childResult;
            try
            {
                childResult = MiniMax(depthTarget - 1, -SearchInfinity, -alpha, 1);
            }
            finally
            {
                UndoTrackedMove(move, undoInfo);
            }

            int score = -childResult.Evaluation;
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                bestLine = [move, .. childResult.BestLine];
            }

            alpha = Math.Max(alpha, score);
        }

        Logger.Debug($"Search depth {depthTarget} selected {StringRepr.MoveToUci(bestMove)} with score {bestScore}.");
        return new SearchResult(bestMove, bestScore, bestLine);
    }

    private SearchResult MiniMax(int depth, int alpha, int beta, int ply)
    {
        if (drawManager.IsThreefoldRepetition() || DrawManager.IsDrawBy50MoveRule(currentBoard))
            return new SearchResult(Move.Null(), 0, []);

        List<Move> legalMoves = MoveGenerator.GenerateMoves(currentBoard);
        if (legalMoves.Count == 0)
        {
            int terminalScore = CheckRestriction.GetCheckingPieces(currentBoard) != 0 ? -MateScore + ply : 0;
            return new SearchResult(Move.Null(), terminalScore, []);
        }

        if (depth == 0)
            return new SearchResult(Move.Null(), Evaluator.EvaluatePosition(currentBoard), []);

        int bestScore = -SearchInfinity;
        List<Move> bestLine = [];
        foreach (Move move in legalMoves)
        {
            UndoInfo undoInfo = ApplyTrackedMove(move);
            SearchResult childResult;
            try
            {
                childResult = MiniMax(depth - 1, -beta, -alpha, ply + 1);
            }
            finally
            {
                UndoTrackedMove(move, undoInfo);
            }

            int score = -childResult.Evaluation;
            if (score > bestScore)
            {
                bestScore = score;
                bestLine = [move, .. childResult.BestLine];
            }

            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
                break;
        }

        return new SearchResult(Move.Null(), bestScore, bestLine);
    }

    private readonly record struct SearchResult(Move BestMove, int Evaluation, List<Move> BestLine);

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
        bool lostCastlingRights = false;
        bool[] currentCastlingRights = currentBoard.GetCastlingRights();
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

    private void UndoTrackedMove(Move move, UndoInfo undoInfo)
    {
        drawManager.PopPosition();
        currentBoard.UndoMove(move, undoInfo);
    }
}
