using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DisCatSharp.Examples.Interactivity.Commands
{
	/// <summary>
	/// Demonstration of interactive commands.
	/// </summary>
	public class InteractivityCommands : ApplicationCommandsModule
	{
		/// <summary>
		/// Wait for message.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		[SlashCommand("message", "Wait for message")]
		public static async Task Message(InteractionContext ctx)
		{
			// Get the Interactivity extension
			var interactivity = ctx.Client.GetInteractivity();

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new(new()
			{
				Content = "Please, send a message"
			}));

			// Here we wait for the same user to send a message to the same channel.
			// You can also add your own conditions here, such as checking the content of the message.
			// The command will not continue until a message is sent that meets the conditions OR until a certain amount of time has passed (in this example, 5 minutes)
			var result = await interactivity.WaitForMessageAsync(msg => msg.Author == ctx.Member, TimeSpan.FromMinutes(5));

			// We should check if the previous method was completed with the expiration of time
			if (result.TimedOut)
			{
				await ctx.EditResponseAsync(new()
				{
					Content = "Timed out!"
				});
				return;
			}

			// result.Result - an original message with which you can do anything, including using its content and deleting.
			await ctx.EditResponseAsync(new()
			{
				Content = result.Result.Content
			});

			await result.Result.DeleteAsync();
		}

		/// <summary>
		/// Wait for reaction.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		[SlashCommand("reaction", "Wait for reaction")]
		public static async Task Reaction(InteractionContext ctx)
		{
			// Get the Interactivity extension
			var interactivity = ctx.Client.GetInteractivity();

			var emoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new(new()
			{
				Content = $"To confirm, select the reaction {emoji}"
			}));

			// Add a reaction to the message, otherwise the user will have to search for and add the desired reaction.
			var msg = await ctx.GetOriginalResponseAsync();
			await msg.CreateReactionAsync(emoji);

			// Here we wait for the user to click on the desired reaction.
			// You can also add your own conditions here.
			// The command will not continue until the reaction is pressed OR until a certain amount of time has passed (in this example, 5 minutes)
			var result = await interactivity.WaitForReactionAsync(react =>
					react.Message == msg && react.User.Id == ctx.Member.Id && react.Emoji == emoji, TimeSpan.FromMinutes(5));

			// We should check if the previous method was completed with the expiration of time
			if (result.TimedOut)
			{
				await ctx.EditResponseAsync(new()
				{
					Content = "Timed out!"
				});
				return;
			}

			// result.Result - arguments that contain information such as the message, channel, and emoji of the clicked reaction.
			await ctx.EditResponseAsync(new()
			{
				Content = $"You added a reaction: {emoji}"
			});
		}

		/// <summary>
		/// Wait for button.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		[SlashCommand("button", "Wait for button")]
		public static async Task Button(InteractionContext ctx)
		{
			// Get the Interactivity extension
			var interactivity = ctx.Client.GetInteractivity();

			// Here we create a list of buttons that will be added to the message later.
			var buttons = new List<DiscordButtonComponent>
			{
				new(ButtonStyle.Primary, "btn1", "Button 1", false,
					new(DiscordEmoji.FromName(ctx.Client, ":one:"))),

				new(ButtonStyle.Secondary, "btn2", "Button 2", false,
					new(DiscordEmoji.FromName(ctx.Client, ":two:")))
			};

			var response = new DiscordMessageBuilder()
			{
				Content = "Click on one of the buttons below."
			};

			// Add a list of buttons to message builder.
			response.AddComponents(buttons);

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new(response));

			// Here we wait for the user to click on one of the buttons.
			// The command will not continue until the button is pressed OR until a certain amount of time has passed (in this example, 5 minutes)
			var result = await interactivity.WaitForButtonAsync(await ctx.GetOriginalResponseAsync(), ctx.Member, TimeSpan.FromMinutes(5));

			// We should check if the previous method was completed with the expiration of time
			if (result.TimedOut)
			{
				await ctx.EditResponseAsync(new()
				{
					Content = "Timed out!"
				});
				return;
			}

			// result.Result.Id - button id
			await ctx.EditResponseAsync(new()
			{
				Content = $"You pressed the {(result.Result.Id == "btn1" ? "first" : "second")} button"
			});
		}

		/// <summary>
		/// Wait for select menu.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		[SlashCommand("select_menu", "Wait for select menu")]
		public static async Task SelectMenu(InteractionContext ctx)
		{
			// Get the Interactivity extension
			var interactivity = ctx.Client.GetInteractivity();

			// Create a list of options that will be available in the select menu
			var options = new List<DiscordStringSelectComponentOption>
			{
				new("one", "One", "", false, new(DiscordEmoji.FromName(ctx.Client, ":one:"))),
				new("two", "Two", "", false, new(DiscordEmoji.FromName(ctx.Client, ":two:"))),
				new("three", "Three", "", false, new(DiscordEmoji.FromName(ctx.Client, ":three:")))
			};

			var response = new DiscordMessageBuilder()
			{
				Content = "Choose something from the select menu below."
			};

			// Like the rest of the components, we add select menu to the message using AddComponents
			response.AddComponents(new DiscordStringSelectComponent("select_menu", "Choose something", options));

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new(response));
			var msg = await ctx.GetOriginalResponseAsync();

			// Wait for the user who used the command to select something from the menu.
			// The command will not continue until something is selected OR until a certain amount of time has passed (in this example, 5 minutes)
			var result = await interactivity.WaitForSelectAsync(msg, ctx.Member, "select_menu", ComponentType.StringSelect, TimeSpan.FromMinutes(5));

			// We should check if the previous method was completed with the expiration of time
			if (result.TimedOut)
			{
				await ctx.EditResponseAsync(new()
				{
					Content = "Timed out!"
				});
				return;
			}

			// result.Result.Values[] - the array of Id of all selected items.
			await ctx.EditResponseAsync(new()
			{
				Content = "You selected " + result.Result.Values[0]
			});
		}

		/// <summary>
		/// Waiting for a button to be pressed after executing a command.
		/// </summary>
		/// <param name="ctx">Interaction context</param>
		[SlashCommand("random", "Waiting for a button to be pressed after executing a command")]
		public static async Task Random(InteractionContext ctx)
		{
			// In this example, we only create a message with the components, their processing will take place in the event handler in the Program class.

			var buttons = new List<DiscordButtonComponent>
			{
				new(ButtonStyle.Danger, "rand_cancel", "Cancel", false,
					new(DiscordEmoji.FromName(ctx.Client, ":stop_button:"))),

				new(ButtonStyle.Success, "rand_next", "Next", false,
					new(DiscordEmoji.FromName(ctx.Client, ":arrow_forward:")))
			};

			var random = new Random();

			var builder = new DiscordMessageBuilder
			{
				Content = random.Next(0, 100).ToString()
			};
			builder.AddComponents(buttons);

			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new(builder));
		}
	}
}