

using Core.DataStructures;

namespace Engine.Evaluation;

public class DrawManager
{

    // Threefold repetition section
    private readonly List<ulong> positions = [];
    private readonly Stack<int> previousRepetitionStartIndices = [];
    private int repetitionStartIndex;

    public ulong CurrentPositionHash => positions.Count > 0
        ? positions[^1]
        : throw new InvalidOperationException("The position history has not been initialized.");

    public void ResetPositions(ulong newPosHash)
    {
        positions.Clear();
        previousRepetitionStartIndices.Clear();
        positions.Add(newPosHash);
        repetitionStartIndex = 0;
    }

    public void PushPosition(ulong posHash, bool isIrreversible)
    {
        previousRepetitionStartIndices.Push(repetitionStartIndex);
        positions.Add(posHash);
        if (isIrreversible)
            repetitionStartIndex = positions.Count - 1;
    }

    public void PopPosition()
    {
        if (positions.Count <= 1 || previousRepetitionStartIndices.Count == 0)
            throw new InvalidOperationException("There is no position to undo.");

        positions.RemoveAt(positions.Count - 1);
        repetitionStartIndex = previousRepetitionStartIndices.Pop();
    }

    public bool IsThreefoldRepetition()
    {
        if (positions.Count == 0)
            throw new InvalidOperationException("The position history has not been initialized.");

        ulong currentPosition = positions[^1];
        int occurrences = 0;
        for (int i = repetitionStartIndex; i < positions.Count; i++)
        {
            if (positions[i] == currentPosition && ++occurrences == 3)
                return true;
        }

        return false;
    }

    // Other draws section
    public static bool IsDrawBy50MoveRule(Board board)
    {
        return board.halfmoveClock >= 100;
    }

    public static bool IsDrawByInsufficientMaterial(Board board)
    {
        // TODO: Recognize every endgame drawn by insufficient material
        throw new NotImplementedException();
    }
}