namespace FileConverter.HelperClasses
{
    /// <summary>
    /// Class contains functions that make printing out text to the console better formatted 
    /// </summary>
    static class PrintHelper
    {
        public static ConsoleColor OldCol { get; set; }
        /// <summary>
        /// Prints a message in the specified color
        /// </summary>
        /// <param name="message"> message to be printed </param>
        /// <param name="c"> the color </param>
        public static void PrintLn(string message, ConsoleColor c = ConsoleColor.White)
        {
            Console.ForegroundColor = c;
            Console.WriteLine(message);
            Console.ForegroundColor = OldCol;
        }

        /// <summary>
        /// Prints a message in the specified color with arguments
        /// </summary>
        /// <param name="format"> message with for example {0} in it</param>
        /// <param name="c"> the color </param>
        public static void PrintLn(string format, ConsoleColor c = ConsoleColor.White, params object[] args)
        {
            string formattedString = string.Format(format, args);
            Console.ForegroundColor = c;
            Console.WriteLine(formattedString);
            Console.ForegroundColor = OldCol;
        }


        /// <summary>
        /// Prints a message in the specified color with Write instead of WriteLine
        /// </summary>
        /// <param name="message"> the message </param>
        /// <param name="c"> the color </param>
        public static void Print(string message, ConsoleColor c = ConsoleColor.White)
        {
            Console.ForegroundColor = c;
            Console.Write(message);
            Console.ForegroundColor = OldCol;
        }
    }
}