using System;

namespace SlashCommands
{
    public class Tag
    {
        // The tag name.
        public string Name { get; internal set; }

        // The tag's content.
        public string Content { get; internal set; }

        // Which guild the tag belongs too.
        public ulong GuildId { get; internal set; }

        // Who owns the tag.
        public ulong OwnerId { get; internal set; }

        // When the tag was created at.
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}