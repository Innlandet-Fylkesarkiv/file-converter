namespace FileConverter.HelperClasses
{
    static class PrintHelper
    {
        public static ConsoleColor OldCol { get; set; }
        public static void PrintLn(string message, ConsoleColor c = ConsoleColor.White)
        {
            Console.ForegroundColor = c;
            Console.WriteLine(message);
            Console.ForegroundColor = OldCol;
        }
        public static void PrintLn(string format, ConsoleColor c = ConsoleColor.White, params object[] args)
        {
            string formattedString = string.Format(format, args);
            Console.ForegroundColor = c;
            Console.WriteLine(formattedString);
            Console.ForegroundColor = OldCol;
        }
        public static void Print(string message, ConsoleColor c = ConsoleColor.White)
        {
            Console.ForegroundColor = c;
            Console.Write(message);
            Console.ForegroundColor = OldCol;
        }
    }
}