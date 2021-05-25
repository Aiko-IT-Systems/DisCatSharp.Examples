using DSharpPlus.Examples.Bots.Main;
using System;
using System.Reflection;

namespace DSharpPlus.Examples.Bots
{
    class Program
    {
        static void Main(string[] args = null)
        {
            string dev = "ODIyMjQyNDQ0MDcwMDkyODYw.YFPa8w.bqrtuN3t7L1FQ9kRsm08clbJDUo";
            Console.WriteLine($"{Assembly.GetExecutingAssembly().FullName.Split(",")[0]} has the permission to run. Yay!");
            using var b = new Bot(dev);
            b.RunAsync().Wait();
        }
    }
}
