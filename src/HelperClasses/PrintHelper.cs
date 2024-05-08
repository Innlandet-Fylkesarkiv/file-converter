namespace FileConverter.HelperClasses
{
    static class PrintHelper
    {
        public static ConsoleColor OldCol { get; set; }
        public static void PrintLn(string message, ConsoleColor c)
        {
            Console.ForegroundColor = c;
            Console.WriteLine(message);
            Console.ForegroundColor = OldCol;
        }
        public static void PrintLn(string format, ConsoleColor c, params object[] args)
        {
            string formattedString = string.Format(format, args);
            Console.ForegroundColor = c;
            Console.WriteLine(formattedString);
            Console.ForegroundColor = OldCol;
        }
        public static void Print(string message, ConsoleColor c)
        {
            Console.ForegroundColor = c;
            Console.Write(message);
            Console.ForegroundColor = OldCol;
        }
    }
}