using System;
using System.Collections.Concurrent;

namespace DisCatSharp.Examples.Hosting.Services;

/// <summary>
///     Tracks lightweight runtime metrics for the hosting example.
/// </summary>
/// <param name="timeProvider">The time provider used for timestamps.</param>
public sealed class BotStatusService(TimeProvider timeProvider)
{
	private readonly ConcurrentDictionary<string, int> _commandCounts = new(StringComparer.OrdinalIgnoreCase);
	private readonly TimeProvider _timeProvider = timeProvider;

	/// <summary>
	///     Gets when the hosting example started up.
	/// </summary>
	public DateTimeOffset StartedAt { get; } = timeProvider.GetUtcNow();

	/// <summary>
	///     Records a command invocation and returns a snapshot of the runtime state.
	/// </summary>
	/// <param name="botName">The logical bot name.</param>
	/// <param name="commandName">The command name.</param>
	/// <param name="gatewayLatency">The current gateway latency.</param>
	/// <param name="guildCount">How many guilds the bot is in.</param>
	/// <returns>The resulting runtime snapshot.</returns>
	public BotStatusSnapshot RecordCommand(string botName, string commandName, int gatewayLatency, int guildCount)
	{
		var invocationCount = this._commandCounts.AddOrUpdate($"{botName}:{commandName}", 1, static (_, current) => current + 1);
		return new(botName, commandName, gatewayLatency, guildCount, invocationCount, this.StartedAt, this._timeProvider.GetUtcNow());
	}
}

/// <summary>
///     Represents a point-in-time runtime snapshot for a hosted bot command.
/// </summary>
/// <param name="BotName">The logical bot name.</param>
/// <param name="CommandName">The command name.</param>
/// <param name="GatewayLatency">The gateway latency at capture time.</param>
/// <param name="GuildCount">The known guild count.</param>
/// <param name="InvocationCount">The number of times this command has been called.</param>
/// <param name="StartedAt">When the host started.</param>
/// <param name="CapturedAt">When this snapshot was captured.</param>
public sealed record BotStatusSnapshot(
	string BotName,
	string CommandName,
	int GatewayLatency,
	int GuildCount,
	int InvocationCount,
	DateTimeOffset StartedAt,
	DateTimeOffset CapturedAt)
{
	/// <summary>
	///     Gets how long the host has been running.
	/// </summary>
	public TimeSpan Uptime => this.CapturedAt - this.StartedAt;
}
