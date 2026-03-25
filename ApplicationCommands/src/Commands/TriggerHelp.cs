using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
///     Shows advanced usage of ChoiceProvider attribute with Reflection.
///     Notice how Ping inherits the ApplicationCommandsModule.
/// </summary>
public class TriggerHelp : ApplicationCommandsModule
{
	/// <summary>
	///     Slash command registers the name and command description.
	/// </summary>
	/// <param name="context">Interaction context</param>
	/// <param name="commandName">The name of the command to get help on</param>
	[SlashCommand("trigger_help", "Sends the help menu for the bot.")]
	public static async Task CommandAsync(
		InteractionContext context,
		// ChoiceProvider calls the Provider() method, which gives a list of slash commands. This is called once, when commands are being registered to Discord.
		[ChoiceProvider(typeof(TriggerHelpChoiceProvider)), Option("command", "The name of the command to get help on.")]
		string commandName
	)
	{
		// Using the TriggerHelpChoiceProvider class, we know that whatever the user gives us *should* be in the Commands dictionary, provided that they use tab completion. If they don't, say we couldn't find the command.
		if (!TriggerHelpChoiceProvider.Commands.TryGetValue(commandName, out var command))
		{
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = $"Error: Command {commandName.Sanitize().InlineCode()} not found!",
				IsEphemeral = true
			});
			return;
		}

		var slashCommandAttribute = command.GetCustomAttribute<SlashCommandAttribute>();
		var parameterLines = new List<string>();

		foreach (var parameter in command.GetParameters())
		{
			var parameterChoice = parameter.GetCustomAttribute<OptionAttribute>(false);
			if (parameterChoice == null)
				continue;

			parameterLines.Add($"- {(parameter.IsOptional ? "Optional" : "Required")} `{parameterChoice.Name}` · `{parameter.ParameterType.Name}` — {parameterChoice.Description}");
		}

		var header = new DiscordSectionComponent(
		[
			new DiscordTextDisplayComponent($"## /{commandName}"),
			new DiscordTextDisplayComponent(slashCommandAttribute.Description)
		]);

		if (context.Guild?.IconUrl != null)
			// CHALLENGE: Replace the jpg to the highest resolution png file using the Discord API.
			header.WithThumbnailComponent(context.Guild.IconUrl, $"{context.Guild.Name} icon");

		// CHALLENGE: Group commands by feature area or add concrete usage examples once the help surface grows.
		var card = new DiscordContainerComponent(accentColor: new DiscordColor("#7b84d1"))
			.AddComponent(header)
			.AddComponent(new DiscordTextDisplayComponent(parameterLines.Count == 0
				? "- No slash-command options are required for this command."
				: string.Join(Environment.NewLine, parameterLines)))
			.AddComponent(new DiscordTextDisplayComponent("> Reflection keeps this help card tiny even as you add more commands."));

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.WithV2Components()
			.AddComponents(card));
	}
}

/// <summary>
///     Choice provider for manage_permissions command
/// </summary>
public sealed class TriggerHelpChoiceProvider : IChoiceProvider
{
	internal static readonly Dictionary<string, MethodInfo> Commands = [];

	/// <summary>
	///     Using Reflection, we search our program for ApplicationCommandsModules and register them as commands.
	/// </summary>
	/// <returns>A list of application slash commands.</returns>
	public Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
	{
		var commandClasses = Assembly.GetEntryAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(ApplicationCommandsModule)) && !type.IsNested);

		foreach (var command in commandClasses)
			SearchCommands(command);

		var discordApplicationCommandOptionChoices = Commands.Keys.Select(commandName => new DiscordApplicationCommandOptionChoice(commandName, commandName)).ToList();
		discordApplicationCommandOptionChoices.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		return Task.FromResult(discordApplicationCommandOptionChoices.AsEnumerable());
	}

	/// <summary>
	///     Adding all commands and subcommands to Commands field.
	/// </summary>
	/// <param name="type">Type of the command.</param>
	/// <param name="commandName">Name of the command.</param>
	public static void SearchCommands(Type type, string commandName = "")
	{
		var nestedTypes = type.GetNestedTypes().Where(innerType => innerType?.GetCustomAttribute<SlashCommandGroupAttribute>() != null).ToList();
		if (nestedTypes.Count != 0)
			foreach (var nestedType in nestedTypes)
			{
				var slashCommandGroupAttribute = nestedType.GetCustomAttribute<SlashCommandGroupAttribute>();
				commandName += ' ' + slashCommandGroupAttribute.Name;
				SearchCommands(nestedType, commandName);
			}
		else
		{
			var commands = type.GetMethods().Where(method => method.GetCustomAttribute<SlashCommandAttribute>() != null).ToList();
			if (commands.Count == 0)
				return;

			foreach (var command in commands)
			{
				var slashCommandAttribute = command.GetCustomAttribute<SlashCommandAttribute>();
				var subCommand = commandName + ' ' + slashCommandAttribute.Name;
				Commands.Add(subCommand.Trim(), command);
			}
		}
	}
}
