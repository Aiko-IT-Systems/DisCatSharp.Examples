using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

namespace DisCatSharp.Examples.Interactivity.Commands;

/// <summary>
///     Demonstration of interactive commands.
/// </summary>
public class InteractivityCommands : ApplicationCommandsModule
{
	private static DiscordContainerComponent CreateCard(string title, IEnumerable<string> lines, string footer = null, string accentColor = "#5865F2")
	{
		var card = new DiscordContainerComponent(accentColor: new DiscordColor(accentColor))
			.AddComponent(new DiscordTextDisplayComponent($"## {title}"))
			.AddComponent(new DiscordTextDisplayComponent(string.Join(Environment.NewLine, lines.Select(static line => $"- {line}"))));

		if (!string.IsNullOrWhiteSpace(footer))
			card.AddComponent(new DiscordTextDisplayComponent($"> {footer}"));

		return card;
	}

	private static DiscordActionRowComponent CreateRow(params DiscordComponent[] components)
		=> new(components);

	private static DiscordInteractionResponseBuilder CreateV2Response(params DiscordComponent[] components)
		=> new DiscordInteractionResponseBuilder().WithV2Components().AddComponents(components);

	private static DiscordWebhookBuilder CreateV2Webhook(params DiscordComponent[] components)
		=> new DiscordWebhookBuilder().WithV2Components().ClearComponents().AddComponents(components);

	private static DiscordFollowupMessageBuilder CreateV2Followup(params DiscordComponent[] components)
		=> new DiscordFollowupMessageBuilder().WithV2Components().AddComponents(components);

	private static string GetModalTextValue(DiscordInteraction interaction, string customId, string fallback = "[Not provided]")
	{
		// CHALLENGE: Expand this helper to read select, checkbox, or file-upload values so one modal can capture richer state.
		var value = interaction.Data.ModalComponents
			.OfType<DiscordLabelComponent>()
			.Select(static component => component.Component)
			.OfType<DiscordTextInputComponent>()
			.FirstOrDefault(component => component.CustomId == customId)?
			.Value;

		return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
	}

	/// <summary>
	///     Wait for message.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("message", "Wait for message", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task Message(InteractionContext ctx)
	{
		var interactivity = ctx.Client.GetInteractivity();

		// CHALLENGE: Add message-content validation and return a friendly Components V2 error card when the input does not match your rules.
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(CreateCard("Message capture",
		[
			"Send your next message in this channel and the bot will echo it back as captured input.",
			"This is a simple pattern for onboarding, support forms, or intake notes."
		], "Validate the content before accepting it once you're ready for a stricter intake flow.")));

		var result = await interactivity.WaitForMessageAsync(msg => msg.Author.Id == ctx.User.Id && msg.ChannelId == ctx.Channel.Id, TimeSpan.FromMinutes(5));
		if (result.TimedOut)
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Message capture timed out",
			[
				"No follow-up message arrived within 5 minutes."
			], "Try shortening the timeout or sending a reminder before it expires.", "#ED4245")));
			return;
		}

		await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Message captured",
		[
			$"Captured text: {result.Result.Content}",
			$"Captured from: {ctx.User.Mention}"
		], "Delete or transform the message next if you want a cleaner intake flow.", "#57F287")));

		await result.Result.DeleteAsync();
	}

	/// <summary>
	///     Wait for reaction.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("reaction", "Wait for reaction", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task Reaction(InteractionContext ctx)
	{
		var interactivity = ctx.Client.GetInteractivity();
		var emoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(CreateCard("Reaction confirmation",
		[
			$"React with {emoji} on this message to confirm the prompt.",
			"Reaction-based flows are still useful when you want the message itself to stay visually lightweight."
		], "Compare this with the button example to see when components feel more explicit.")));

		var msg = await ctx.GetOriginalResponseAsync();
		await msg.CreateReactionAsync(emoji);

		var result = await interactivity.WaitForReactionAsync(react =>
			react.Message == msg && react.User.Id == ctx.User.Id && react.Emoji == emoji, TimeSpan.FromMinutes(5));

		if (result.TimedOut)
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Reaction timed out",
			[
				$"Nobody confirmed with {emoji} before the timeout."
			], "Buttons are often the clearer modern alternative when you own the message.", "#ED4245")));
			return;
		}

		await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Reaction confirmed",
		[
			$"{ctx.User.Mention} confirmed the prompt with {emoji}.",
			"Use this result object to branch into another action or edit the message further."
		], "Next step: swap the reaction for a button and compare the UX.", "#57F287")));
	}

	/// <summary>
	///     Wait for button.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("button", "Wait for button", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task Button(InteractionContext ctx)
	{
		var interactivity = ctx.Client.GetInteractivity();
		// CHALLENGE: Encode a lightweight state object into custom IDs and teach users how to safely parse it back out.
		var row = CreateRow(
			new DiscordButtonComponent(ButtonStyle.Primary, "btn1", "Button 1", false, new(DiscordEmoji.FromName(ctx.Client, ":one:"))),
			new DiscordButtonComponent(ButtonStyle.Secondary, "btn2", "Button 2", false, new(DiscordEmoji.FromName(ctx.Client, ":two:"))));

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(
			CreateCard("Button prompt",
			[
				"Choose one of the buttons below to continue the flow.",
				"Buttons are great when the next actions should be obvious and mutually exclusive."
			], "Try adding a destructive button once you want to demonstrate confirmations."),
			row));

		var result = await interactivity.WaitForButtonAsync(await ctx.GetOriginalResponseAsync(), ctx.User, TimeSpan.FromMinutes(5));
		if (result.TimedOut)
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Button prompt timed out",
			[
				"No button was clicked before the timeout."
			], "Disable stale buttons in longer-lived flows so users know the panel expired.", "#ED4245")));
			return;
		}

		await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Button result",
		[
			$"You pressed the {(result.Result.Id == "btn1" ? "first" : "second")} button.",
			$"Component ID: `{result.Result.Id}`"
		], "Branch on custom IDs when you want richer state transitions.", "#57F287")));
	}

	/// <summary>
	///     Wait for select menu.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("select_menu", "Wait for select menu", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task SelectMenu(InteractionContext ctx)
	{
		var interactivity = ctx.Client.GetInteractivity();
		// CHALLENGE: Replace this string select with an entity select once you want to teach channel, role, or user pickers.
		var options = new List<DiscordStringSelectComponentOption>
		{
			new("one", "One", "Pick the first option", false, new(DiscordEmoji.FromName(ctx.Client, ":one:"))),
			new("two", "Two", "Pick the second option", false, new(DiscordEmoji.FromName(ctx.Client, ":two:"))),
			new("three", "Three", "Pick the third option", false, new(DiscordEmoji.FromName(ctx.Client, ":three:")))
		};

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(
			CreateCard("Select-menu prompt",
			[
				"Choose a value from the select menu below.",
				"Select menus work well for categories, priorities, and longer lists."
			], "Next challenge: swap this for entity selects or add multi-select support."),
			CreateRow(new DiscordStringSelectComponent("Choose something", options, "select_menu"))));

		var msg = await ctx.GetOriginalResponseAsync();
		var result = await interactivity.WaitForSelectAsync(msg, ctx.User, "select_menu", ComponentType.StringSelect, TimeSpan.FromMinutes(5));
		if (result.TimedOut)
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Select menu timed out",
			[
				"The menu expired before a choice was made."
			], "Consider a follow-up reminder if the choice is important.", "#ED4245")));
			return;
		}

		await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Selection captured",
		[
			$"You selected `{result.Result.Values[0]}`.",
			"Read the Values collection when you allow multiple selections."
		], "Try chaining another select menu to build a richer wizard.", "#57F287")));
	}

	/// <summary>
	///     Waiting for a button to be pressed after executing a command.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("random", "Waiting for a button to be pressed after executing a command", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task Random(InteractionContext ctx)
	{
		var ownerId = ctx.User.Id;
		// CHALLENGE: Persist the latest random result per user so the button flow survives reconnects or bot restarts.
		var buttons = CreateRow(
			new DiscordButtonComponent(ButtonStyle.Danger, $"rand_cancel:{ownerId}", "Cancel", false, new(DiscordEmoji.FromName(ctx.Client, ":stop_button:"))),
			new DiscordButtonComponent(ButtonStyle.Success, $"rand_next:{ownerId}", "Next", false, new(DiscordEmoji.FromName(ctx.Client, ":arrow_forward:"))));

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(
			CreateCard("Owner-scoped randomizer",
			[
				$"Current result: `{System.Random.Shared.Next(0, 100)}`",
				$"Only {ctx.User.Mention} can continue or cancel this panel."
			], "This demonstrates a component flow that keeps working after the command body exits."),
			buttons));
	}

	/// <summary>
	///     Runs a practical multi-step task intake flow with selects, buttons, modal capture, and follow-up actions.
	/// </summary>
	/// <param name="ctx">Interaction context</param>
	[SlashCommand("workflow", "Runs a richer multi-step task intake flow.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task Workflow(InteractionContext ctx)
	{
		var interactivity = ctx.Client.GetInteractivity();
		var priorityId = $"workflow_priority:{ctx.User.Id}";
		var soloId = $"workflow_solo:{ctx.User.Id}";
		var reviewId = $"workflow_review:{ctx.User.Id}";
		var summaryModalId = $"workflow_summary_modal:{ctx.User.Id}:{ctx.InteractionId}";
		var summaryFieldId = $"workflow_summary_text:{ctx.User.Id}";
		var checkpointFieldId = $"workflow_checkpoint_text:{ctx.User.Id}";
		var shipId = $"workflow_ship:{ctx.User.Id}";
		var blockId = $"workflow_block:{ctx.User.Id}";

		var options = new List<DiscordStringSelectComponentOption>
		{
			new("Low", "low", "Nice to have", false, new(DiscordEmoji.FromName(ctx.Client, ":large_blue_circle:"))),
			new("Medium", "medium", "Useful soon", false, new(DiscordEmoji.FromName(ctx.Client, ":large_yellow_circle:"))),
			new("High", "high", "Needs action today", false, new(DiscordEmoji.FromName(ctx.Client, ":red_circle:")))
		};

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(
			CreateCard("Task intake · Step 1/4",
			[
				"Pick a priority for the work item you want to capture.",
				"We'll use the next steps to simulate how a real moderation or project queue might behave."
			], "This example keeps a little learning-space so you can add persistence later."),
			CreateRow(new DiscordStringSelectComponent("Choose a priority", options, priorityId))));

		var message = await ctx.GetOriginalResponseAsync();
		var priority = await interactivity.WaitForSelectAsync(message, ctx.User, priorityId, ComponentType.StringSelect, TimeSpan.FromMinutes(5));
		if (priority.TimedOut)
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Task intake timed out",
			[
				"No priority was selected before the timeout."
			], "Try prompting again or narrowing the number of choices.", "#ED4245")));
			return;
		}

		var selectedPriority = priority.Result.Values[0];
		await priority.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, CreateV2Response(
			CreateCard("Task intake · Step 2/4",
			[
				$"Priority captured: `{selectedPriority}`",
				"Choose whether you'll handle the item yourself or send it for review.",
				"The route button you click next will open a modal for the richer task details."
			], "Buttons are a good fit for branching the next step in a workflow."),
			CreateRow(
				new DiscordButtonComponent(ButtonStyle.Success, soloId, "Take it solo", false, new(DiscordEmoji.FromName(ctx.Client, ":runner:"))),
				new DiscordButtonComponent(ButtonStyle.Secondary, reviewId, "Needs review", false, new(DiscordEmoji.FromName(ctx.Client, ":eyes:"))))));

		var route = await interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromMinutes(2));
		if (route.TimedOut || (route.Result.Id != soloId && route.Result.Id != reviewId))
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Task intake timed out",
			[
				"No route was chosen before the timeout."
			], "Consider disabling expired buttons in any production flow.", "#ED4245")));
			return;
		}

		var selectedRoute = route.Result.Id == soloId ? "Solo execution" : "Send for review";
		// CHALLENGE: Split this modal into two versions so solo work and review work collect different fields.
		await route.Result.Interaction.CreateInteractionModalResponseAsync(new DiscordInteractionModalBuilder("Task intake details", summaryModalId)
			.AddTextDisplayComponent(new DiscordTextDisplayComponent($$"""
				### Task intake · Step 3/4
				- Priority: `{{selectedPriority}}`
				- Route: `{{selectedRoute}}`
				- Capture the narrative details in the fields below.
				"""))
			.AddLabelComponent(new DiscordLabelComponent("Task summary", "Describe the work item in one or two sentences.")
				.WithTextComponent(new DiscordTextInputComponent(TextComponentStyle.Paragraph, customId: summaryFieldId, placeholder: "Refresh the onboarding flow and wire the final CTA to the status endpoint.", minLength: 10, maxLength: 500)))
			.AddLabelComponent(new DiscordLabelComponent("Immediate next checkpoint", "Optional: what should happen right after capture?")
				.WithTextComponent(new DiscordTextInputComponent(TextComponentStyle.Small, customId: checkpointFieldId, placeholder: "Pair with reviewer / gather missing assets", minLength: 3, maxLength: 100, required: false))));

		var summary = await interactivity.WaitForModalAsync(summaryModalId, TimeSpan.FromMinutes(5));
		if (summary.TimedOut)
		{
			await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Task intake timed out",
			[
				"The modal was never submitted."
			], "You could reopen the modal from a fresh button click or persist unfinished state for later.", "#ED4245")));
			return;
		}

		var summaryText = GetModalTextValue(summary.Result.Interaction, summaryFieldId, "[No summary provided]");
		var checkpointText = GetModalTextValue(summary.Result.Interaction, checkpointFieldId);

		// CHALLENGE: Reject invalid modal submissions here and reopen the modal with the user's previous values still filled in.
		await summary.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateV2Response(CreateCard("Modal captured",
		[
			$"Summary: {summaryText}",
			$"Next checkpoint: {checkpointText}"
		], "The original workflow card and the follow-up panel will continue from here.", "#57F287")).AsEphemeral());

		var followup = await ctx.FollowUpAsync(CreateV2Followup(
			CreateCard("Task journey · Step 4/4",
			[
				$"Summary: {summaryText}",
				$"Next checkpoint: {checkpointText}",
				$"Priority: `{selectedPriority}`",
				$"Route: `{selectedRoute}`",
				$"Captured for: {ctx.User.Mention}"
			], "Use the buttons below to record the immediate outcome."),
			CreateRow(
				new DiscordButtonComponent(ButtonStyle.Success, shipId, "Ship today", false, new(DiscordEmoji.FromName(ctx.Client, ":rocket:"))),
				new DiscordButtonComponent(ButtonStyle.Secondary, blockId, "Mark blocked", false, new(DiscordEmoji.FromName(ctx.Client, ":construction:"))))));

		await ctx.EditResponseAsync(CreateV2Webhook(CreateCard("Task intake complete",
		[
			$"Priority captured: `{selectedPriority}`",
			$"Route captured: `{selectedRoute}`",
			$"Modal checkpoint: {checkpointText}",
			"A follow-up panel was posted below to continue the workflow."
		], "This command now demonstrates select menus, branching buttons, modal capture, and follow-up handling.", "#57F287")));

		var outcome = await interactivity.WaitForButtonAsync(followup, ctx.User, TimeSpan.FromMinutes(2));
		if (outcome.TimedOut || (outcome.Result.Id != shipId && outcome.Result.Id != blockId))
		{
			await ctx.EditFollowupAsync(followup.Id, CreateV2Webhook(CreateCard("Task journey paused",
			[
				$"Summary: {summaryText}",
				$"Next checkpoint: {checkpointText}",
				"No outcome button was pressed before the timeout."
			], "Try persisting the workflow state so users can return later.", "#ED4245")));
			return;
		}

		var shipped = outcome.Result.Id == shipId;
		// CHALLENGE: Save this outcome to a thread, JSON file, or database so the flow becomes a real lightweight task tracker.
		await outcome.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, CreateV2Response(CreateCard(
			shipped ? "Task marked ready" : "Task marked blocked",
			[
				$"Summary: {summaryText}",
				$"Next checkpoint: {checkpointText}",
				$"Priority: `{selectedPriority}`",
				$"Route: `{selectedRoute}`",
				shipped ? "Outcome: `Ready to ship today`" : "Outcome: `Blocked and needs follow-up`"
			],
			"Use this as a launch point for your own workflow extensions.",
			shipped ? "#3BA55D" : "#ED4245")));
	}
}
