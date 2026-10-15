namespace Core.Helpers
{
    /*
        A collection of enums used throughout the chess engine to represent various game elements and states.
    */
    public enum PieceType
    {
        None = -1,
        Pawn = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4,
        King = 5
    }
    public enum SideColor
    {
        White = 0,
        Black = 1
    }
    public enum PieceName
    {
        None = -1,
        WhitePawn = 0,
        WhiteKnight = 1,
        WhiteBishop = 2,
        WhiteRook = 3,
        WhiteQueen = 4,
        WhiteKing = 5,
        BlackPawn = 6,
        BlackKnight = 7,
        BlackBishop = 8,
        BlackRook = 9,
        BlackQueen = 10,
        BlackKing = 11
    }
    public enum CastlingRightsIndex
    {
        WhiteKingSide = 0,
        WhiteQueenSide = 1,
        BlackKingSide = 2,
        BlackQueenSide = 3
    }
    public enum GameState
    {
        InProgress = 0,
        WhiteWin = 1,
        BlackWin = 2,
        Draw = 3
    }
    public enum CaptureType
    {
        None = 0,
        Normal = 1,
        EnPassant = 2
    }
    public enum CastlingType
    {
        None = 0,
        KingSide = 1,
        QueenSide = 2
    }
}
