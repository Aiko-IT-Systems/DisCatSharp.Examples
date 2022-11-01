using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Threading.Tasks;

namespace DisCatSharp.Examples.Interactivity.Commands
{
	/// <summary>
	/// Shows how to create, archive and delete threads.
	/// </summary>
	public class ThreadCommands : ApplicationCommandsModule
	{
		/// <summary>
		/// Create a thread in a specific channel.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		/// <param name="name">Thread name</param>
		/// <param name="channel">The channel where the thread should be created</param>
		[SlashCommand("create_thread", "Create a thread in a specific channel")]
		public static async Task CreateThread(InteractionContext ctx, [Option("name", "Thread name")] string name, [Option("channel", "The channel where the thread should be created")] DiscordChannel channel = null)
		{
			channel ??= ctx.Channel;

			// Create a thread without a message
			await channel.CreateThreadAsync(name);

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new(new()
			{
				Content = $"{name} was successfully created in the {channel.Mention}."
			}));
		}

		/// <summary>
		/// Archive thread.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		/// <param name="threadId">Thread Id</param>
		[SlashCommand("archive_thread", "Archive thread")]
		public static async Task ArchiveThread(InteractionContext ctx, [Option("thread", "Thread id")] string threadId)
		{
			// Get the thread from its Id
			var thread = ctx.Guild.GetThread(ulong.Parse(threadId));

			// Check whether the desired thread was found.
			if (thread == null)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
				{
					Content = "Thread not found."
				});
				return;
			}

			await thread.ArchiveAsync();

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"{thread.Name} has been successfully archived."
			});
		}

		/// <summary>
		/// Permanently delete thread.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		/// <param name="threadId">Thread Id</param>
		[SlashCommand("delete_thread", "Permanently delete thread (deletes all content of the thread)")]
		public static async Task DeleteThread(InteractionContext ctx, [Option("thread", "Thread id")] string threadId)
		{
			// Get the thread from its Id
			var thread = ctx.Guild.GetThread(ulong.Parse(threadId));

			// Check whether the desired thread was found.
			if (thread == null)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
				{
					Content = "Thread not found."
				});
				return;
			}

			await thread.DeleteAsync();

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"{thread.Name} has been successfully deleted."
			});
		}
	}
}