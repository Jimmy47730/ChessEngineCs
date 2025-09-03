using Core.Helpers;

namespace Core.Board
{
    public class Square
    {
        
        public static int Index(string squareName)
        {
            if (string.IsNullOrWhiteSpace(squareName) || squareName == "-")
            {
                return -1; // No square
            }
            return StringRepr.SquareConversion(squareName);
        }
        public static int Index(int rank, int file) => rank * 8 + file;
        public static string Name(int squareData) => StringRepr.SquareConversion(squareData);
        public static string Name(int file, int rank) => StringRepr.SquareConversion(rank * 8 + file);

        
    }
}