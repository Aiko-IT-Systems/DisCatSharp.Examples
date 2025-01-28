using System;

namespace DisCatSharp.Examples.ApplicationCommands;

public sealed class Tag
{
	// The tag name.
	public string Name { get; internal init; }

	// The tag's content.
	public string Content { get; internal init; }

	// Which guild the tag belongs too.
	public ulong GuildId { get; internal init; }

	// Who owns the tag.
	public ulong OwnerId { get; internal init; }

	// When the tag was created at.
	public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
