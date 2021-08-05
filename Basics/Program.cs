using DisCatSharp.Examples.Basics.Main;

namespace DisCatSharp.Examples.Basics
{
    /// <summary>
    /// The program.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">The args.</param>
        static void Main(string[] args)
        {
            string token = args[0];
            using var bot = new Bot(token);
            bot.RunAsync().Wait();
        }
    }
}
