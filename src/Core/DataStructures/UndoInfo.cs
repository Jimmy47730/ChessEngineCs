using Core.Helpers;

namespace Core.DataStructures;

public readonly struct UndoInfo(
    char capturedPiece,
    int capturedSquare,
    int previousEnPassantSquare,
    int previousCastlingRights,
    int previousHalfmoveClock,
    int previousFullmoveNumber,
    SideColor previousSideToMove,
    ulong previousZobristHash
)
{
    public readonly char CapturedPiece = capturedPiece;
    public readonly int CapturedSquare = capturedSquare;
    public readonly int PreviousEnPassantSquare = previousEnPassantSquare;
    // Pack the four rights into bits, with lsb being castling right index 0
    public readonly int PreviousCastlingRights = previousCastlingRights;
    public readonly int PreviousHalfmoveClock = previousHalfmoveClock;
    public readonly int PreviousFullmoveNumber = previousFullmoveNumber;
    public readonly SideColor PreviousSideToMove = previousSideToMove;
    public readonly ulong PreviousZobristHash = previousZobristHash;
}