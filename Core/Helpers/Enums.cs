namespace Core.Helpers
{
    public enum PieceType
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6
    }
    public enum SideColor
    {
        White = 0,
        Black = 1
    }
    public enum PieceName
    {
        None = 0,
        WhitePawn = 1,
        WhiteKnight = 2,
        WhiteBishop = 3,
        WhiteRook = 4,
        WhiteQueen = 5,
        WhiteKing = 6,
        BlackPawn = 7,
        BlackKnight = 8,
        BlackBishop = 9,
        BlackRook = 10,
        BlackQueen = 11,
        BlackKing = 12
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

    public enum MoveType
    {
        Normal = 0,
        EnPassant = 1,
        ShortCastle = 2,
        LongCastle = 3,
        Promotion = 4
    }
    public enum PromotionType
    {
        None = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4
    }
    public enum CheckType
    {
        None = 0,
        Check = 1,
        Checkmate = 2
    }
}
