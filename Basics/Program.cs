using DSharpPlusNextGen.Examples.Bots.Basics.Main;
using System;
using System.Reflection;

namespace DSharpPlusNextGen.Examples.Bots.Basics
{
    class Program
    {
        static void Main(string[] args = null)
        {
            string dev = "";
            Console.WriteLine($"{Assembly.GetExecutingAssembly().FullName.Split(",")[0]} has the permission to run. Yay!");
            using var b = new Bot(dev);
            b.RunAsync().Wait();
        }
    }
}
