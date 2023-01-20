using System;

namespace MarineHell
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello, World!");

            var bot = new MarineHell();
            bot.Run();

            Console.ReadLine();
        }
    }
}