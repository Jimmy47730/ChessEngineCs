using Chess;
using Core.DataStructures;
using Core.Helpers;
using Engine.Evaluation;
using Engine.MoveGeneration;

namespace Engine.Search;

public class Searcher(Board board, DrawManager drawManager)
{
    private const int MateScore = 1_000_000;
    private const int SearchInfinity = 1_100_000;
    private const int DefaultSearchDepth = 8;
    private readonly Board board = board;
    private readonly DrawManager drawManager = drawManager;

    public Board Board => board;

    public SearchResult Search(int depthTarget = DefaultSearchDepth, CancellationToken cancellationToken = default)
    {
        if (depthTarget < 1)
            throw new ArgumentOutOfRangeException(nameof(depthTarget), "Search depth must be at least one.");

        List<Move> legalMoves = MoveGenerator.GenerateMoves(board);
        Logger.Debug($"Generated {legalMoves.Count} legal moves.");
        if (legalMoves.Count == 0)
            return new SearchResult(Move.Null(), 0, []);

        Move bestMove = legalMoves[0];
        int bestScore = -SearchInfinity;
        List<Move> bestLine = [];
        int alpha = -SearchInfinity;

        foreach (Move move in legalMoves)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            UndoInfo undoInfo = ApplyTrackedMove(move);
            SearchResult childResult;
            try
            {
                childResult = MiniMax(depthTarget - 1, -SearchInfinity, -alpha, 1, cancellationToken);
            }
            finally
            {
                UndoTrackedMove(move, undoInfo);
            }

            if (childResult.Cancelled)
                break;

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

    private SearchResult MiniMax(int depth, int alpha, int beta, int ply, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return SearchResult.CancelledResult;

        if (drawManager.IsThreefoldRepetition() || DrawManager.IsDrawBy50MoveRule(board))
            return new SearchResult(Move.Null(), 0, []);

        List<Move> legalMoves = MoveGenerator.GenerateMoves(board);
        if (legalMoves.Count == 0)
        {
            int terminalScore = CheckRestriction.GetCheckingPieces(board) != 0 ? -MateScore + ply : 0;
            return new SearchResult(Move.Null(), terminalScore, []);
        }

        if (depth == 0)
            return new SearchResult(Move.Null(), Evaluator.EvaluatePosition(board), []);

        int bestScore = -SearchInfinity;
        List<Move> bestLine = [];
        foreach (Move move in legalMoves)
        {
            if (cancellationToken.IsCancellationRequested)
                return SearchResult.CancelledResult;

            UndoInfo undoInfo = ApplyTrackedMove(move);
            SearchResult childResult;
            try
            {
                childResult = MiniMax(depth - 1, -beta, -alpha, ply + 1, cancellationToken);
            }
            finally
            {
                UndoTrackedMove(move, undoInfo);
            }

            if (childResult.Cancelled)
                return SearchResult.CancelledResult;

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

    private UndoInfo ApplyTrackedMove(Move move)
    {
        char movedPiece = board.GetPieceAt(move.From());
        bool[] previousCastlingRights = board.GetCastlingRights();
        UndoInfo undoInfo = board.ApplyMove(move);
        bool[] currentCastlingRights = board.GetCastlingRights();
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
        drawManager.PushPosition(board.zobristHash, isIrreversible);
        return undoInfo;
    }

    private void UndoTrackedMove(Move move, UndoInfo undoInfo)
    {
        drawManager.PopPosition();
        board.UndoMove(move, undoInfo);
    }

    public readonly record struct SearchResult(Move BestMove, int Evaluation, List<Move> BestLine, bool Cancelled = false)
    {
        public static SearchResult CancelledResult => new(Move.Null(), 0, [], true);
    }
}
