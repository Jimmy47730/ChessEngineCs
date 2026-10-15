using Chess;
using Core.Helpers;
using Core.DataStructures;
using Engine.MoveGeneration;
namespace Engine;

public class Bot
{
    public bool IsThinking { get; private set; }
    private Board currentBoard;
    public Action<string>? OnMoveChosen;

    // Bot implementation

    public void NotifyNewGame()
    {
        currentBoard = new Board();
    }

    public void SetPosition(string fen = "")
    {
        if (string.IsNullOrEmpty(fen))
            currentBoard = new Board();
        else
            currentBoard = new Board(fen);
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
        throw new NotImplementedException();
    }

    public void ApplyMove(string moveUci)
    {
        throw new NotImplementedException();
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
        IsThinking = true;
        List<Move> legalMoves = MoveGenerator.GenerateMoves(currentBoard);
        Logger.Debug($"Generated {legalMoves.Count} legal moves.");
        if (legalMoves.Count == 0)
        {
            Logger.Warn("No legal moves available.");
            OnMoveChosen?.Invoke("0000");
            IsThinking = false;
            return;
        }
        Random rng = new();
        Move bestMove = legalMoves[rng.Next(legalMoves.Count)];

        /*
        Logger.Debug("All legal moves: ");
        foreach (var move in legalMoves)
        {
            Logger.Debug($"{move} ({move.ToUci()})");
        }
        */
        Logger.Debug($"Legal moves found: {legalMoves.Count}");
        OnSearchComplete(bestMove);
        IsThinking = false;
    }

    private void OnSearchComplete(Move bestMove)
    {
        string bestMoveUci = StringRepr.MoveToUci(bestMove);
        Logger.Info($"Chosen move: {bestMove} ({bestMoveUci})");
        OnMoveChosen?.Invoke(bestMoveUci);
    }
}
