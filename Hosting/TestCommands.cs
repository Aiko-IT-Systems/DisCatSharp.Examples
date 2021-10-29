using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

namespace Hosting
{
    public class TestCommands : BaseCommandModule
    {
        [Command("test"), Description("Test command for Hosting"), RequireDisCatSharpDeveloper]
        public async Task TestAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("Test successfull");
        }
    }
}
