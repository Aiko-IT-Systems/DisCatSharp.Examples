using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DisCatSharp.Entities;
using DisCatSharp.Voice;
using DisCatSharp.Voice.Entities;
using DisCatSharp.Voice.Enums;
using DisCatSharp.Voice.EventArgs;

using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.VoiceNext;

internal static class VoiceRecordingStore
{
	private static readonly ConcurrentDictionary<ulong, VoiceRecordingSession> s_sessions = [];

	public static VoiceRecordingSession Get(ulong guildId)
		=> s_sessions.TryGetValue(guildId, out var session) ? session : null;

	public static bool TryStart(DiscordGuild guild, VoiceConnection connection, ILogger logger, out VoiceRecordingSession session)
	{
		session = new VoiceRecordingSession(guild, connection, logger);
		if (s_sessions.TryAdd(guild.Id, session))
		{
			session.Attach();
			return true;
		}

		session.DisposeAsync().AsTask().GetAwaiter().GetResult();
		return false;
	}

	public static async Task<VoiceRecordingSummary> StopAsync(ulong guildId) => !s_sessions.TryRemove(guildId, out var session) ? null : await session.StopAsync();
}

internal sealed class VoiceRecordingSession : IAsyncDisposable
{
	private readonly string _captureDirectory;
	private readonly VoiceConnection _connection;
	private readonly ConcurrentDictionary<ulong, SpeakerCapture> _captures = [];
	private readonly ConcurrentDictionary<VoicePacketDropReason, int> _dropReasons = [];
	private readonly DiscordGuild _guild;
	private readonly ILogger _logger;
	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
	private VoiceRecordingSummary _completedSummary;
	private int _concealmentFrames;
	private int _droppedPackets;
	private int _formatMismatchCount;
	private int _framesReceived;
	private long _pcmBytes;
	private int _stopped;

	public VoiceRecordingSession(DiscordGuild guild, VoiceConnection connection, ILogger logger)
	{
		this._guild = guild;
		this._connection = connection;
		this._logger = logger;
		this.StartedAt = DateTimeOffset.UtcNow;
		this._captureDirectory = Path.Combine(AppContext.BaseDirectory, "recordings", guild.Id.ToString(), this.StartedAt.ToString("yyyyMMdd-HHmmss"));
	}

	public string CaptureDirectory => this._captureDirectory;

	public TimeSpan Elapsed => this._stopwatch.Elapsed;

	public int DroppedPackets => Volatile.Read(ref this._droppedPackets);

	public int FramesReceived => Volatile.Read(ref this._framesReceived);

	public int ConcealmentFrames => Volatile.Read(ref this._concealmentFrames);

	public int FormatMismatchCount => Volatile.Read(ref this._formatMismatchCount);

	public long PcmBytes => Interlocked.Read(ref this._pcmBytes);

	public DateTimeOffset StartedAt { get; }

	public IReadOnlyDictionary<VoicePacketDropReason, int> DropReasons => this._dropReasons;

	public IReadOnlyList<SpeakerCaptureSnapshot> GetSpeakerSnapshots()
		=> [.. this._captures.Values
			.Select(static capture => capture.GetSnapshot())
			.OrderByDescending(static capture => capture.BytesWritten)
			.ThenBy(static capture => capture.DisplayName, StringComparer.OrdinalIgnoreCase)];

	public void Attach()
	{
		Directory.CreateDirectory(this._captureDirectory);
		this._connection.VoiceReceived += this.OnVoiceReceivedAsync;
		this._connection.VoicePacketDropped += this.OnVoicePacketDroppedAsync;
	}

	public async Task<VoiceRecordingSummary> StopAsync()
	{
		if (Interlocked.Exchange(ref this._stopped, 1) == 1)
			return this._completedSummary;

		this._connection.VoiceReceived -= this.OnVoiceReceivedAsync;
		this._connection.VoicePacketDropped -= this.OnVoicePacketDroppedAsync;
		this._stopwatch.Stop();

		var totalTimelineMs = Math.Max(0, (int)Math.Ceiling(this.Elapsed.TotalMilliseconds));
		var files = new List<RecordedSpeakerFile>();
		foreach (var capture in this._captures.Values.OrderBy(static capture => capture.DisplayName, StringComparer.OrdinalIgnoreCase))
			files.Add(await capture.CompleteAsync(totalTimelineMs));

		var notes = new List<string>
		{
			"Tracks are timeline-aware: each speaker file starts at the recording session start and inserts silence for receive gaps before later packets.",
			"Timing is anchored to when decoded frames reached the bot, so the files are practical for later editor alignment but not guaranteed to be sample-accurate under network jitter."
		};

		if (this.FormatMismatchCount > 0)
			notes.Add($"Ignored {this.FormatMismatchCount} frame(s) because the sender changed format mid-recording.");

		if (this.DroppedPackets > 0)
			notes.Add($"Dropped {this.DroppedPackets} packet(s) before decoding. Check the drop breakdown in `/record_status` while debugging.");

		// CHALLENGE: Persist this summary to JSON or a database so moderators can review capture metadata after the bot restarts.
		this._completedSummary = new(
			this._guild.Id,
			this._connection.TargetChannel?.Name ?? "Unknown voice channel",
			this.StartedAt,
			this.Elapsed,
			totalTimelineMs,
			this.FramesReceived,
			this.ConcealmentFrames,
			this.DroppedPackets,
			this.PcmBytes,
			this.FormatMismatchCount,
			files,
			notes,
			this._captureDirectory);

		this._logger.LogInformation(
			"Voice recording finished for guild {GuildId}. Speakers: {SpeakerCount}; Frames: {FrameCount}; Bytes: {ByteCount}; Timeline: {TimelineMs}ms",
			this._guild.Id,
			files.Count,
			this.FramesReceived,
			this.PcmBytes,
			totalTimelineMs);

		return this._completedSummary;
	}

	public async ValueTask DisposeAsync()
	{
		if (this._completedSummary == null)
			await this.StopAsync();
	}

	private Task OnVoicePacketDroppedAsync(VoiceConnection sender, VoicePacketDroppedEventArgs eventArgs)
	{
		if (Volatile.Read(ref this._stopped) == 1)
			return Task.CompletedTask;

		Interlocked.Increment(ref this._droppedPackets);
		this._dropReasons.AddOrUpdate(eventArgs.Reason, 1, static (_, count) => count + 1);
		return Task.CompletedTask;
	}

	private Task OnVoiceReceivedAsync(VoiceConnection sender, VoiceReceiveEventArgs eventArgs)
	{
		if (Volatile.Read(ref this._stopped) == 1 || eventArgs.User == null || eventArgs.PcmData.IsEmpty)
			return Task.CompletedTask;

		Interlocked.Increment(ref this._framesReceived);
		if (eventArgs.IsConcealmentFrame)
			Interlocked.Increment(ref this._concealmentFrames);

		Interlocked.Add(ref this._pcmBytes, eventArgs.PcmData.Length);

		var capture = this._captures.GetOrAdd(eventArgs.User.Id, _ => SpeakerCapture.Create(this._captureDirectory, eventArgs.User, eventArgs.AudioFormat));
		if (!capture.Matches(eventArgs.AudioFormat))
		{
			Interlocked.Increment(ref this._formatMismatchCount);
			return Task.CompletedTask;
		}

		var frameReceivedAt = this._stopwatch.Elapsed;
		var timelineStartMs = Math.Max(0, (int)Math.Round(frameReceivedAt.TotalMilliseconds - eventArgs.AudioDuration));

		// CHALLENGE: Replace the receive-time anchor with RTP timestamp reconstruction if you want tighter sync for post-production tooling.
		// CHALLENGE: Skip near-silent PCM frames here if you want smaller files and a cleaner moderation review queue.
		return capture.WriteAtAsync(eventArgs.PcmData, timelineStartMs, eventArgs.AudioDuration, eventArgs.IsConcealmentFrame);
	}
}

internal sealed class SpeakerCapture : IAsyncDisposable
{
	private const int BitsPerSample = 16;
	private const int SilenceBufferSize = 8192;
	private const int WaveHeaderSize = 44;
	private readonly FileStream _stream;
	private readonly SemaphoreSlim _writeLock = new(1, 1);
	private int _concealmentFrames;
	private int _frameCount;
	private int _initialTimelineOffsetMs = -1;
	private long _pcmBytes;
	private int _silencePaddingMs;
	private int _timelineLengthMs;

	private SpeakerCapture(ulong userId, string displayName, AudioFormat audioFormat, string filePath)
	{
		this.UserId = userId;
		this.DisplayName = displayName;
		this.AudioFormat = audioFormat;
		this.FilePath = filePath;
		this._stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		this._stream.Write(new byte[WaveHeaderSize]);
	}

	public AudioFormat AudioFormat { get; }

	public string DisplayName { get; }

	public string FilePath { get; }

	public ulong UserId { get; }

	public int ConcealmentFrames => Volatile.Read(ref this._concealmentFrames);

	public int FrameCount => Volatile.Read(ref this._frameCount);

	public int InitialTimelineOffsetMs => Volatile.Read(ref this._initialTimelineOffsetMs);

	public long PcmBytes => Interlocked.Read(ref this._pcmBytes);

	public int SilencePaddingMs => Volatile.Read(ref this._silencePaddingMs);

	public int TimelineLengthMs => Volatile.Read(ref this._timelineLengthMs);

	public static SpeakerCapture Create(string captureDirectory, DiscordUser user, AudioFormat audioFormat)
	{
		Directory.CreateDirectory(captureDirectory);
		var displayName = string.IsNullOrWhiteSpace(user.GlobalName) ? user.Username : user.GlobalName;
		var safeName = SanitizeFileName(displayName);
		var filePath = Path.Combine(captureDirectory, $"{safeName}-{user.Id}.wav");
		return new SpeakerCapture(user.Id, displayName, audioFormat, filePath);
	}

	public SpeakerCaptureSnapshot GetSnapshot()
		=> new(
			this.UserId,
			this.DisplayName,
			this.PcmBytes,
			this.FrameCount,
			this.TimelineLengthMs,
			this.InitialTimelineOffsetMs,
			this.SilencePaddingMs,
			this.AudioFormat.SampleRate,
			this.AudioFormat.ChannelCount);

	public bool Matches(AudioFormat incomingFormat)
		=> this.AudioFormat.SampleRate == incomingFormat.SampleRate && this.AudioFormat.ChannelCount == incomingFormat.ChannelCount;

	public async Task WriteAtAsync(ReadOnlyMemory<byte> pcmData, int timelineStartMs, int audioDurationMs, bool isConcealmentFrame)
	{
		await this._writeLock.WaitAsync();
		try
		{
			if (this._initialTimelineOffsetMs < 0)
				this._initialTimelineOffsetMs = timelineStartMs;

			if (timelineStartMs > this._timelineLengthMs)
			{
				var silenceGapMs = timelineStartMs - this._timelineLengthMs;
				await WriteSilenceAsync(this._stream, silenceGapMs, this.AudioFormat);
				this._timelineLengthMs += silenceGapMs;
				this._silencePaddingMs += silenceGapMs;
				this._pcmBytes += CalculatePcmByteCount(silenceGapMs, this.AudioFormat);
			}

			await this._stream.WriteAsync(pcmData);
			this._pcmBytes += pcmData.Length;
			this._timelineLengthMs = Math.Max(this._timelineLengthMs, timelineStartMs + audioDurationMs);
			this._frameCount++;
			if (isConcealmentFrame)
				this._concealmentFrames++;
		}
		finally
		{
			this._writeLock.Release();
		}
	}

	public async Task<RecordedSpeakerFile> CompleteAsync(int totalTimelineMs)
	{
		await this._writeLock.WaitAsync();
		try
		{
			if (totalTimelineMs > this._timelineLengthMs)
			{
				var trailingSilenceMs = totalTimelineMs - this._timelineLengthMs;
				await WriteSilenceAsync(this._stream, trailingSilenceMs, this.AudioFormat);
				this._timelineLengthMs += trailingSilenceMs;
				this._silencePaddingMs += trailingSilenceMs;
				this._pcmBytes += CalculatePcmByteCount(trailingSilenceMs, this.AudioFormat);
			}

			await this._stream.FlushAsync();
			WriteWaveHeader(this._stream, this.PcmBytes, this.AudioFormat.SampleRate, this.AudioFormat.ChannelCount);
			await this._stream.FlushAsync();
		}
		finally
		{
			this._writeLock.Release();
		}

		await this._stream.DisposeAsync();
		this._writeLock.Dispose();
		return new(
			this.UserId,
			this.DisplayName,
			this.FilePath,
			this.PcmBytes,
			this.FrameCount,
			this.ConcealmentFrames,
			this.TimelineLengthMs,
			this.InitialTimelineOffsetMs < 0 ? 0 : this.InitialTimelineOffsetMs,
			this.SilencePaddingMs,
			this.AudioFormat.SampleRate,
			this.AudioFormat.ChannelCount);
	}

	public async ValueTask DisposeAsync()
	{
		await this._stream.DisposeAsync();
		this._writeLock.Dispose();
	}

	private static int CalculatePcmByteCount(int durationMs, AudioFormat audioFormat)
	{
		if (durationMs <= 0)
			return 0;

		var sampleCount = audioFormat.SampleRate * durationMs / 1000;
		return sampleCount * audioFormat.ChannelCount * (BitsPerSample / 8);
	}

	private static string SanitizeFileName(string value)
	{
		var invalidCharacters = Path.GetInvalidFileNameChars();
		var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '-' : character).ToArray()).Trim();
		return string.IsNullOrWhiteSpace(sanitized) ? "speaker" : sanitized;
	}

	private static void WriteWaveHeader(Stream stream, long pcmBytes, int sampleRate, int channelCount)
	{
		var byteRate = sampleRate * channelCount * (BitsPerSample / 8);
		var blockAlign = channelCount * (BitsPerSample / 8);
		var riffSize = checked((int)(pcmBytes + 36));
		var dataSize = checked((int)pcmBytes);
		Span<byte> header = stackalloc byte[WaveHeaderSize];
		"RIFF"u8.CopyTo(header);
		BinaryPrimitives.WriteInt32LittleEndian(header[4..8], riffSize);
		"WAVEfmt "u8.CopyTo(header[8..16]);
		BinaryPrimitives.WriteInt32LittleEndian(header[16..20], 16);
		BinaryPrimitives.WriteInt16LittleEndian(header[20..22], 1);
		BinaryPrimitives.WriteInt16LittleEndian(header[22..24], checked((short)channelCount));
		BinaryPrimitives.WriteInt32LittleEndian(header[24..28], sampleRate);
		BinaryPrimitives.WriteInt32LittleEndian(header[28..32], byteRate);
		BinaryPrimitives.WriteInt16LittleEndian(header[32..34], checked((short)blockAlign));
		BinaryPrimitives.WriteInt16LittleEndian(header[34..36], BitsPerSample);
		"data"u8.CopyTo(header[36..40]);
		BinaryPrimitives.WriteInt32LittleEndian(header[40..44], dataSize);
		stream.Position = 0;
		stream.Write(header);
	}

	private static async Task WriteSilenceAsync(Stream stream, int durationMs, AudioFormat audioFormat)
	{
		var byteCount = CalculatePcmByteCount(durationMs, audioFormat);
		if (byteCount <= 0)
			return;

		var silenceBuffer = new byte[Math.Min(byteCount, SilenceBufferSize)];
		var remaining = byteCount;
		while (remaining > 0)
		{
			var chunkSize = Math.Min(remaining, silenceBuffer.Length);
			await stream.WriteAsync(silenceBuffer.AsMemory(0, chunkSize));
			remaining -= chunkSize;
		}
	}
}

internal sealed record RecordedSpeakerFile(
	ulong UserId,
	string DisplayName,
	string FilePath,
	long PcmBytes,
	int FrameCount,
	int ConcealmentFrames,
	int TimelineLengthMs,
	int InitialTimelineOffsetMs,
	int SilencePaddingMs,
	int SampleRate,
	int ChannelCount);

internal sealed record SpeakerCaptureSnapshot(
	ulong UserId,
	string DisplayName,
	long BytesWritten,
	int FrameCount,
	int TimelineLengthMs,
	int InitialTimelineOffsetMs,
	int SilencePaddingMs,
	int SampleRate,
	int ChannelCount);

internal sealed record VoiceRecordingSummary(
	ulong GuildId,
	string ChannelName,
	DateTimeOffset StartedAt,
	TimeSpan Duration,
	int TimelineLengthMs,
	int FramesReceived,
	int ConcealmentFrames,
	int DroppedPackets,
	long PcmBytes,
	int FormatMismatchCount,
	IReadOnlyList<RecordedSpeakerFile> Files,
	IReadOnlyList<string> Notes,
	string CaptureDirectory);
