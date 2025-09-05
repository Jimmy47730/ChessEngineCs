using Chess;
using Core.Board;
using Core.Helpers;
using Engine.MoveGeneration;
namespace Engine;

public class Bot
{
    public bool IsThinking { get; private set; }
    private BoardState currentBoard;
    public Action<string>? OnMoveChosen;

    // Bot implementation

    public void NotifyNewGame()
    {
        currentBoard = FenUtility.ReadFen();
    }

    public void SetPosition(string fen)
    {
        currentBoard = FenUtility.ReadFen(fen);
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
        currentBoard = currentBoard.ApplyMove(move);
    }

    public void ApplyMove(string moveUci)
    {
        currentBoard = currentBoard.ApplyMove(moveUci);
    }

    public void StopThinking()
    {
        throw new NotImplementedException();
    }

    // Time is always treated in milliseconds
    public int DefineThinkingTime(int whiteTime, int blackTime, int whiteInc, int blackInc)
    {
        // Temporary implementation
        return whiteTime / 20 + whiteInc;
    }

    public void Think(int time)
    {
        IsThinking = true;
        PseudoLegal moveGen = PseudoLegal.Instance();
        List<Move> legalMoves = moveGen.GenerateMoves(currentBoard);
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

        Logger.Debug("All legal moves: ");
        foreach (var move in legalMoves)
        {
            Logger.Debug($"{move} ({move.ToUci()})");
        }
        OnSearchComplete(bestMove);
        IsThinking = false;
    }

    private void OnSearchComplete(Move bestMove)
    {
        Logger.Info($"Chosen move: {bestMove} ({bestMove.ToUci()})");
        string bestMoveUci = bestMove.ToUci();
        OnMoveChosen?.Invoke(bestMoveUci);
    }
}
