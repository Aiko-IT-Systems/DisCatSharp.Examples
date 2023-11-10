using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.VoiceNext;

using System.Threading.Tasks;

namespace DisCatSharp.Examples.VoiceNext.Commands;

/// <summary>
/// Commands to connect and disconnect to the voice channel.
/// </summary>
public class ConnectionCommands : ApplicationCommandsModule
{
	/// <summary>
	/// Connect to the voice channel.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("connect", "Join the voice channel")]
	public static async Task ConnectAsync(InteractionContext ctx)
	{
		// Check if the user is currently connected to the voice channel
		if (ctx.Member.VoiceState == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "You must be connected to a voice channel to use this command!"
			});
			return;
		}

		// Connect to the channel
		_ = ctx.Member.VoiceState.Channel.ConnectAsync();

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"The bot has joined the channel {ctx.Member.VoiceState.Channel.Name.InlineCode()}"
		});
	}

	/// <summary>
	/// Disconnect from the voice channel.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("leave", "Leave the voice channel")]
	public static async Task LeaveAsync(InteractionContext ctx)
	{
		// Get the current VoiceNext connection in the guild.
		var vnext = ctx.Client.GetVoiceNext();
		var connection = vnext.GetConnection(ctx.Guild);

		// Check if the bot is currently connected to the voice channel
		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "The bot is not connected to the voice channel in this guild!"
			});
			return;
		}

		connection.Disconnect();

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = "The bot left the voice channel"
		});
	}
}