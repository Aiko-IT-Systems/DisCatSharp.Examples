using System;

namespace DisCatSharp.Examples.ApplicationCommands;

public sealed class Tag
{
	/// <summary>
	///     Gets the tag name.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	///     Gets the tag content.
	/// </summary>
	public required string Content { get; init; }

	/// <summary>
	///     Gets the guild the tag belongs to.
	/// </summary>
	public required ulong GuildId { get; init; }

	/// <summary>
	///     Gets the tag owner.
	/// </summary>
	public required ulong OwnerId { get; init; }

	/// <summary>
	///     Gets when the tag was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	/// <summary>
	///     Gets how often the tag has been used.
	/// </summary>
	public int UseCount { get; private set; }

	/// <summary>
	///     Gets when the tag was last used.
	/// </summary>
	public DateTimeOffset? LastUsedAt { get; private set; }

	/// <summary>
	///     Marks the tag as used.
	/// </summary>
	public void MarkUsed()
	{
		this.UseCount++;
		this.LastUsedAt = DateTimeOffset.UtcNow;
	}
}
