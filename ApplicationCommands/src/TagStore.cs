using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DisCatSharp.Examples.ApplicationCommands;

/// <summary>
///     Stores tags per guild for the lifetime of the sample process.
/// </summary>
public sealed class TagStore
{
	private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, Tag>> _tags = new();

	/// <summary>
	///     Normalizes a tag name.
	/// </summary>
	/// <param name="name">The provided name.</param>
	/// <returns>The normalized tag name.</returns>
	public static string NormalizeName(string name)
		=> name.Trim().ToLowerInvariant();

	/// <summary>
	///     Gets a snapshot of the current tags for a guild.
	/// </summary>
	/// <param name="guildId">The guild ID.</param>
	/// <returns>The tags for the guild.</returns>
	public IReadOnlyList<Tag> List(ulong guildId)
		=> [.. this.GetGuildTags(guildId).Values.OrderByDescending(tag => tag.UseCount).ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)];

	/// <summary>
	///     Checks whether a tag already exists.
	/// </summary>
	/// <param name="guildId">The guild ID.</param>
	/// <param name="name">The tag name.</param>
	/// <returns>Whether the tag exists.</returns>
	public bool Contains(ulong guildId, string name)
		=> this.GetGuildTags(guildId).ContainsKey(NormalizeName(name));

	/// <summary>
	///     Gets a tag if it exists.
	/// </summary>
	/// <param name="guildId">The guild ID.</param>
	/// <param name="name">The tag name.</param>
	/// <returns>The tag if found.</returns>
	public Tag Get(ulong guildId, string name)
		=> this.GetGuildTags(guildId).TryGetValue(NormalizeName(name), out var tag) ? tag : null;

	/// <summary>
	///     Attempts to create a new tag.
	/// </summary>
	/// <param name="tag">The tag to create.</param>
	/// <returns>Whether the tag was created.</returns>
	public bool TryCreate(Tag tag)
		=> this.GetGuildTags(tag.GuildId).TryAdd(NormalizeName(tag.Name), tag);

	/// <summary>
	///     Attempts to delete a tag.
	/// </summary>
	/// <param name="guildId">The guild ID.</param>
	/// <param name="name">The tag name.</param>
	/// <param name="tag">The removed tag, if one existed.</param>
	/// <returns>Whether the tag was removed.</returns>
	public bool TryDelete(ulong guildId, string name, out Tag tag)
		=> this.GetGuildTags(guildId).TryRemove(NormalizeName(name), out tag);

	/// <summary>
	///     Marks a tag as used.
	/// </summary>
	/// <param name="guildId">The guild ID.</param>
	/// <param name="name">The tag name.</param>
	/// <param name="tag">The touched tag.</param>
	/// <returns>Whether the tag existed.</returns>
	public bool TryTouch(ulong guildId, string name, out Tag tag)
	{
		if (!this.GetGuildTags(guildId).TryGetValue(NormalizeName(name), out tag))
			return false;

		tag.MarkUsed();
		return true;
	}

	private ConcurrentDictionary<string, Tag> GetGuildTags(ulong guildId)
		=> this._tags.GetOrAdd(guildId, static _ => new(StringComparer.OrdinalIgnoreCase));
}
