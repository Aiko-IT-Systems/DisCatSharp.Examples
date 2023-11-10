using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace DisCatSharp.Examples.Lavalink.Commands;

/// <summary>
/// Playback control with these commands.
/// </summary>
public class MusicCommands : ApplicationCommandsModule
{
	/// <summary>
	/// Play music asynchronously.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	/// <param name="query">Search string or Youtube link</param>
	[SlashCommand("play", "Play music asynchronously")]
	public static async Task PlayAsync(InteractionContext ctx, [Option("query", "Search string or Youtube link")] string query)
	{
		var lava = ctx.Client.GetLavalink();
		var node = lava.ConnectedNodes.Values.First();
		var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "The bot is not connected to the voice channel in this guild!"
			});
			return;
		}

		if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "You must be in the same voice channel as the bot!"
			});
			return;
		}

		LavalinkLoadResult tracks;

		// Check if query is valid url
		if (Uri.TryCreate(query, UriKind.Absolute, out var uriResult) &&
		    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
		{
			// Get track from the url
			tracks = await node.Rest.GetTracksAsync(uriResult);
		}
		else
		{
			// Search track in Youtube
			tracks = await node.Rest.GetTracksAsync(query);
		}

		// If something went wrong on Lavalink's end or it just couldn't find anything.
		if (tracks.LoadResultType is LavalinkLoadResultType.LoadFailed or LavalinkLoadResultType.NoMatches)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = $"Track search failed for `{query}`."
			});
			return;
		}

		// Get first track in the result
		var track = tracks.Tracks.First();

		await connection.PlayAsync(track);

		// CHALLENGE: Add a queue. You need to make sure that new tracks are added to a special queue instead of overwriting the current one
		// and automatically played after the end of the previous track.

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Now playing {track.Author.InlineCode()} - {track.Title.InlineCode()}"
		});
	}

	/// <summary>
	/// Pause playback
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("pause", "Pause playback")]
	public static async Task PauseAsync(InteractionContext ctx)
	{
		var lava = ctx.Client.GetLavalink();
		var node = lava.ConnectedNodes.Values.First();
		var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "The bot is not connected to the voice channel in this guild!"
			});
			return;
		}

		if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "You must be in the same voice channel as the bot!"
			});
			return;
		}

		// Pause playback
		await connection.PauseAsync();

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = "Paused!"
		});
	}

	/// <summary>
	/// Resume playback
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("resume", "Resume playback")]
	public static async Task ResumeAsync(InteractionContext ctx)
	{
		var lava = ctx.Client.GetLavalink();
		var node = lava.ConnectedNodes.Values.First();
		var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "The bot is not connected to the voice channel in this guild!"
			});
			return;
		}

		if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "You must be in the same voice channel as the bot!"
			});
			return;
		}

		// Resume playback
		await connection.ResumeAsync();

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Now playing `{connection.CurrentState.CurrentTrack.Title}`"
		});
	}

	/// <summary>
	/// Stop playback
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("stop", "Stop playback")]
	public static async Task StopAsync(InteractionContext ctx)
	{
		var lava = ctx.Client.GetLavalink();
		var node = lava.ConnectedNodes.Values.First();
		var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "The bot is not connected to the voice channel in this guild!"
			});
			return;
		}

		if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "You must be in the same voice channel as the bot!"
			});
			return;
		}

		await connection.StopAsync();

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = "Playback is stopped!"
		});
	}

	/// <summary>
	/// BONUS: play music through the context menu!
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[ContextMenu(ApplicationCommandType.Message, "Play")]
	public static async Task PlayAsync(ContextMenuContext ctx)
	{
		var query = ctx.TargetMessage.Content;

		var lava = ctx.Client.GetLavalink();
		var node = lava.ConnectedNodes.Values.First();
		var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "The bot is not connected to the voice channel in this guild!"
			});
			return;
		}

		if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = "You must be in the same voice channel as the bot!"
			});
			return;
		}

		LavalinkLoadResult tracks;

		// Check if query is valid url
		if (Uri.TryCreate(query, UriKind.Absolute, out var uriResult) &&
		    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
		{
			// Get track from the url
			tracks = await node.Rest.GetTracksAsync(uriResult);
		}
		else
		{
			// Search track in Youtube
			tracks = await node.Rest.GetTracksAsync(query);
		}

		//If something went wrong on Lavalink's end
		if (tracks.LoadResultType == LavalinkLoadResultType.LoadFailed
		    //or it just couldn't find anything.
		    || tracks.LoadResultType == LavalinkLoadResultType.NoMatches)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				IsEphemeral = true,
				Content = $"Track search failed for `{query}`."
			});
			return;
		}

		// Get first track in the result
		var track = tracks.Tracks.First();

		await connection.PlayAsync(track);

		// CHALLENGE: Add a queue. You need to make sure that new tracks are added to a special queue instead of overwriting the current one
		// and automatically played after the end of the previous track.

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
		{
			Content = $"Now playing {track.Author.InlineCode()} - {track.Title.InlineCode()}"
		});
	}
}