using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Voice;

namespace DisCatSharp.Examples.VoiceNext.Commands;

/// <summary>
///     Commands that demonstrate how to capture incoming voice and turn it into teachable WAV recordings.
/// </summary>
public class RecordingCommands : ApplicationCommandsModule
{
	private static DiscordContainerComponent CreateCard(string title, IEnumerable<string> lines, string footer, string accentColor = "#5865F2")
		=> new DiscordContainerComponent(accentColor: new DiscordColor(accentColor))
			.AddComponent(new DiscordTextDisplayComponent($"## {title}"))
			.AddComponent(new DiscordTextDisplayComponent(string.Join(Environment.NewLine, lines.Select(static line => $"- {line}"))))
			.AddComponent(new DiscordTextDisplayComponent($"> {footer}"));

	private static DiscordInteractionResponseBuilder CreateCardResponse(string title, IEnumerable<string> lines, string footer, string accentColor = "#5865F2")
		=> new DiscordInteractionResponseBuilder().WithV2Components().AddComponents(CreateCard(title, lines, footer, accentColor));

	private static async Task<VoiceConnection> GetConnectionForChannelWorkAsync(InteractionContext ctx)
	{
		var connection = ctx.Client.GetVoice().GetConnection(ctx.Guild);
		if (connection == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				IsEphemeral = true,
				Content = "Connect the bot to a voice channel first with `/connect`."
			});
			return null;
		}

		if (ctx.Member?.VoiceState?.Channel == null || ctx.Member.VoiceState.Channel != connection.TargetChannel)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				IsEphemeral = true,
				Content = "Join the same voice channel as the bot before starting a recording."
			});
			return null;
		}

		return connection;
	}

	[SlashCommand("record_start", "Start capturing incoming voice into one WAV file per speaker.")]
	public static async Task RecordStartAsync(InteractionContext ctx)
	{
		var connection = await GetConnectionForChannelWorkAsync(ctx);
		if (connection == null)
			return;

		if (VoiceRecordingStore.Get(ctx.Guild.Id) != null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				IsEphemeral = true,
				Content = "A recording is already running in this guild. Use `/record_status` or `/record_stop`."
			});
			return;
		}

		if (!VoiceRecordingStore.TryStart(ctx.Guild, connection, ctx.Client.Logger, out _))
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				IsEphemeral = true,
				Content = "Could not start the recording session. Try again in a moment."
			});
			return;
		}

		// CHALLENGE: Add permission checks or approval buttons before starting a capture flow in a real moderation bot.
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateCardResponse("Voice capture armed",
		[
			$"Channel: `{connection.TargetChannel.Name}`",
			$"Decrypt inbound audio: `{connection.IsE2eeUsableForReceive}`",
			"Output: one timeline-aware WAV per speaker so tracks can line up later in an editor.",
			"Silence is inserted for receive gaps instead of collapsing everything into speech-only chunks."
		], "Finish with `/record_stop` when you want the files uploaded back to Discord.", "#57F287"));
	}

	[SlashCommand("record_status", "Show the live status of the active voice recording session.")]
	public static async Task RecordStatusAsync(InteractionContext ctx)
	{
		var session = VoiceRecordingStore.Get(ctx.Guild.Id);
		if (session == null)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				IsEphemeral = true,
				Content = "No recording is currently running. Start one with `/record_start`."
			});
			return;
		}

		var speakerSnapshots = session.GetSpeakerSnapshots();
		var lines = new List<string>
		{
			$"Started: <t:{session.StartedAt.ToUnixTimeSeconds()}:R>",
			$"Elapsed: `{session.Elapsed:g}`",
			$"Frames captured: `{session.FramesReceived}`",
			$"PCM written: `{session.PcmBytes / 1024d:F1} KiB`",
			$"Concealment frames: `{session.ConcealmentFrames}`",
			$"Dropped packets: `{session.DroppedPackets}`"
		};

		if (speakerSnapshots.Count == 0)
		{
			lines.Add("Speakers captured: `0` (wait for someone to talk)");
		}
		else
		{
			lines.Add($"Speakers captured: `{speakerSnapshots.Count}`");
			lines.Add($"Timeline preview: {string.Join(", ", speakerSnapshots.Take(3).Select(snapshot => $"{snapshot.DisplayName} (offset {snapshot.InitialTimelineOffsetMs}ms, silence {snapshot.SilencePaddingMs}ms)"))}");
		}

		if (session.FormatMismatchCount > 0)
			lines.Add($"Format mismatches ignored: `{session.FormatMismatchCount}`");

		// CHALLENGE: Surface the drop-reason breakdown as buttons or a modal so people can triage voice quality without reading logs.
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateCardResponse("Voice capture status", lines,
			"Each per-speaker file keeps a shared session timeline, so gaps stay alignable even though the bot only receives active speech frames.", "#5865F2"));
	}

	[SlashCommand("record_stop", "Stop the active voice recording and upload the captured WAV files.")]
	public static async Task RecordStopAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

		var summary = await VoiceRecordingStore.StopAsync(ctx.Guild.Id);
		if (summary == null)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No active recording is running in this guild."));
			return;
		}

		var uploadedFiles = summary.Files.Take(10).ToList();
		var lines = new List<string>
		{
			$"Channel: `{summary.ChannelName}`",
			$"Duration: `{summary.Duration:g}`",
			$"Shared timeline: `{summary.TimelineLengthMs}ms`",
			$"Speakers recorded: `{summary.Files.Count}`",
			$"Frames captured: `{summary.FramesReceived}`",
			$"Dropped packets: `{summary.DroppedPackets}`",
			$"Files attached: `{uploadedFiles.Count}`"
		};

		if (summary.Files.Count > 0)
			lines.Add($"Track preview: {string.Join(", ", uploadedFiles.Select(file => $"{file.DisplayName} (offset {file.InitialTimelineOffsetMs}ms, silence {file.SilencePaddingMs}ms)"))}");

		if (summary.Notes.Count > 0)
			lines.AddRange(summary.Notes.Select(static note => $"Note: {note}"));

		if (summary.Files.Count > uploadedFiles.Count)
			lines.Add($"Only the first `{uploadedFiles.Count}` files were attached because Discord messages cap attachment counts.");

		if (summary.Files.Count == 0)
			lines.Add("No PCM frames were captured before the recording stopped.");

		var webhook = new DiscordWebhookBuilder().WithV2Components().AddComponents(CreateCard("Voice capture complete", lines,
			"Each attachment is a timeline-aware WAV file, ready for editor alignment, speech-to-text, or later mixing.", "#FEE75C"));

		var fileStreams = new List<FileStream>();
		try
		{
			foreach (var file in uploadedFiles)
			{
				var stream = File.OpenRead(file.FilePath);
				fileStreams.Add(stream);
				webhook.AddFile(Path.GetFileName(file.FilePath), stream, true);
			}

			// CHALLENGE: Zip the per-speaker WAV files or mix them into a shared track if you want a more polished handoff.
			await ctx.EditResponseAsync(webhook);
		}
		finally
		{
			foreach (var stream in fileStreams)
				await stream.DisposeAsync();
		}
	}
}
