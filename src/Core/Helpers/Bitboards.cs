namespace Core.Helpers;

public static class Bitboards
{
    /*
        Helper class with a bunch of predefined bitboards    
    */

    public static readonly ulong FILE_A = 0x0101010101010101;
    public static readonly ulong FILE_B = 0x0202020202020202;
    public static readonly ulong FILE_G = 0x4040404040404040;
    public static readonly ulong FILE_H = 0x8080808080808080;
    public static readonly ulong RANK_1 = 0x00000000000000FF;
    public static readonly ulong RANK_2 = 0x000000000000FF00;
    public static readonly ulong RANK_7 = 0x00FF000000000000;
    public static readonly ulong RANK_8 = 0xFF00000000000000;
    public static readonly ulong FULL = 0xFFFFFFFFFFFFFFFF;
    

}