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
    private const int DefaultSearchDepth = 5;
    private const int MaxCaptureExtensionPly = 6;
    private const int MaxLowMobilityExtensionPly = 5;
    private const int LowMobilityMoveCount = 5;
    private const int RootMovePruningStartDepth = 5;
    private const int RootMoveLimit = 3;
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

        if (cancellationToken.IsCancellationRequested)
            return GetFallbackResult(legalMoves[0]);

        MoveOrdering.OrderMoves(board, legalMoves);
        List<RootMove> rootMoves = new(legalMoves.Count);
        foreach (Move move in legalMoves)
            rootMoves.Add(new RootMove(move));

        SearchResult? completedIteration = null;
        int rootExtensionPly = legalMoves.Count <= LowMobilityMoveCount ? 1 : 0;

        for (int depth = 1; depth <= depthTarget; depth++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            OrderRootMovesFromPreviousIteration(rootMoves);
            int moveCount = depth >= RootMovePruningStartDepth
                ? Math.Min(RootMoveLimit, rootMoves.Count)
                : rootMoves.Count;
            int childDepth = depth - 1 + rootExtensionPly;
            int bestScore = -SearchInfinity;
            Move bestMove = rootMoves[0].Move;
            List<Move> bestLine = [];
            int alpha = -SearchInfinity;
            bool iterationCancelled = false;

            foreach (RootMove rootMove in rootMoves)
                rootMove.CurrentLine = [];

            for (int i = 0; i < moveCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    iterationCancelled = true;
                    break;
                }

                RootMove rootMove = rootMoves[i];
                UndoInfo undoInfo = ApplyTrackedMove(rootMove.Move);
                SearchResult childResult;
                try
                {
                    childResult = MiniMax(
                        childDepth,
                        -SearchInfinity,
                        -alpha,
                        1,
                        rootExtensionPly,
                        cancellationToken);
                }
                finally
                {
                    UndoTrackedMove(rootMove.Move, undoInfo);
                }

                if (childResult.Cancelled)
                {
                    iterationCancelled = true;
                    break;
                }

                int score = -childResult.Evaluation;
                rootMove.CurrentScore = score;
                rootMove.CurrentLine = [rootMove.Move, .. childResult.BestLine];
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = rootMove.Move;
                    bestLine = rootMove.CurrentLine;
                }

                alpha = Math.Max(alpha, score);
                if (IsFastestMateScore(bestScore, ply: 0))
                    break;
            }

            if (bestLine.Count > 0)
            {
                SearchResult iterationResult = new(bestMove, bestScore, bestLine, iterationCancelled);
                completedIteration = iterationResult;

                if (!iterationCancelled)
                {
                    for (int i = 0; i < moveCount; i++)
                        rootMoves[i].PreviousScore = rootMoves[i].CurrentScore;

                    if (depth >= RootMovePruningStartDepth && rootMoves.Count > moveCount)
                        rootMoves.RemoveRange(moveCount, rootMoves.Count - moveCount);
                }
            }

            if (iterationCancelled || IsFastestMateScore(bestScore, ply: 0))
                break;
        }

        SearchResult result = completedIteration ?? GetFallbackResult(rootMoves[0].Move);
        result = result with { Cancelled = result.Cancelled || cancellationToken.IsCancellationRequested };
        Logger.Debug(
            $"Search depth {depthTarget} selected {StringRepr.MoveToUci(result.BestMove)} " +
            $"with score {result.Evaluation}.");
        return result;
    }

    private static void OrderRootMovesFromPreviousIteration(List<RootMove> rootMoves)
    {
        for (int i = 1; i < rootMoves.Count; i++)
        {
            RootMove move = rootMoves[i];
            int j = i - 1;
            while (j >= 0 && rootMoves[j].PreviousScore < move.PreviousScore)
            {
                rootMoves[j + 1] = rootMoves[j];
                j--;
            }

            rootMoves[j + 1] = move;
        }
    }

    private SearchResult GetFallbackResult(Move move)
    {
        UndoInfo undoInfo = board.ApplyMove(move);
        int score;
        try
        {
            score = -Evaluator.EvaluatePosition(board);
        }
        finally
        {
            board.UndoMove(move, undoInfo);
        }

        return new SearchResult(move, score, [move], true);
    }

    private SearchResult MiniMax(
        int depth,
        int alpha,
        int beta,
        int ply,
        int extensionPly,
        CancellationToken cancellationToken)
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

        bool extendForLowMobility =
            legalMoves.Count <= LowMobilityMoveCount &&
            extensionPly < MaxLowMobilityExtensionPly;
        if (depth == 0 && !extendForLowMobility)
            return SearchCaptures(alpha, beta, ply, 0, cancellationToken, legalMoves);

        int childDepth = depth - 1 + (extendForLowMobility ? 1 : 0);
        int childExtensionPly = extensionPly + (extendForLowMobility ? 1 : 0);
        MoveOrdering.OrderMoves(board, legalMoves);
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
                childResult = MiniMax(
                    childDepth,
                    -beta,
                    -alpha,
                    ply + 1,
                    childExtensionPly,
                    cancellationToken);
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
            if (IsFastestMateScore(bestScore, ply) || alpha >= beta)
                break;
        }

        return new SearchResult(Move.Null(), bestScore, bestLine);
    }

    private SearchResult SearchCaptures(
        int alpha,
        int beta,
        int ply,
        int extensionPly,
        CancellationToken cancellationToken,
        List<Move>? knownLegalMoves = null)
    {
        if (cancellationToken.IsCancellationRequested)
            return SearchResult.CancelledResult;

        if (drawManager.IsThreefoldRepetition() || DrawManager.IsDrawBy50MoveRule(board))
            return new SearchResult(Move.Null(), 0, []);

        bool inCheck = CheckRestriction.GetCheckingPieces(board) != 0;
        List<Move> legalMoves = knownLegalMoves ?? MoveGenerator.GenerateMoves(board);
        if (legalMoves.Count == 0)
        {
            int terminalScore = inCheck ? -MateScore + ply : 0;
            return new SearchResult(Move.Null(), terminalScore, []);
        }

        int standPat = Evaluator.EvaluatePosition(board);
        if (extensionPly >= MaxCaptureExtensionPly)
            return new SearchResult(Move.Null(), standPat, []);

        if (!inCheck)
        {
            legalMoves.RemoveAll(move => !move.IsCapture());
            if (legalMoves.Count == 0)
                return new SearchResult(Move.Null(), standPat, []);

            if (standPat >= beta)
                return new SearchResult(Move.Null(), standPat, []);

            alpha = Math.Max(alpha, standPat);
        }

        MoveOrdering.OrderMoves(board, legalMoves);
        int bestScore = inCheck ? -SearchInfinity : standPat;
        List<Move> bestLine = [];

        foreach (Move move in legalMoves)
        {
            if (cancellationToken.IsCancellationRequested)
                return SearchResult.CancelledResult;

            UndoInfo undoInfo = ApplyTrackedMove(move);
            SearchResult childResult;
            try
            {
                childResult = SearchCaptures(
                    -beta,
                    -alpha,
                    ply + 1,
                    extensionPly + 1,
                    cancellationToken);
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
            if (IsFastestMateScore(bestScore, ply) || alpha >= beta)
                break;
        }

        return new SearchResult(Move.Null(), bestScore, bestLine);
    }

    private static bool IsFastestMateScore(int score, int ply)
    {
        return score >= MateScore - ply - 1;
    }

    private sealed class RootMove(Move move)
    {
        public Move Move { get; } = move;
        public int PreviousScore { get; set; } = -SearchInfinity;
        public int CurrentScore { get; set; } = -SearchInfinity;
        public List<Move> CurrentLine { get; set; } = [];
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
