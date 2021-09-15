using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;

namespace DisCatSharp.Examples.ApplicationCommands.Commands
{
    public class ManagePermissions : ApplicationCommandsModule
    {
        [SlashCommandGroup("manage_permissions", "Allows to add/remove permissions to execute commands to users/roles", false)]
        public class Command : ApplicationCommandsModule
        {
            [SlashCommand("add_user", "Add permission to user.")]
            public static async Task UserAdd(InteractionContext context, [Option("user", "User to be added")] DiscordUser user,
                [ChoiceProvider(typeof(ManagePermissionsChoiceProvider)), Option("command", "The command to which we must give access")] string commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                DiscordApplicationCommand command = GetCommand(context, commandName);

                List<DiscordApplicationCommandPermission> perms = new();

                try
                {
                    // Get all current command permissions
                    perms = (await context.Client.GetApplicationCommandPermissionAsync(context.Guild.Id, command.Id))
                        .Permissions.ToList();
                }
                catch (NotFoundException)
                {
                    // Ignore if permissions not found
                }

                // Add user to the permissions
                perms.Add(new(user.Id, ApplicationCommandPermissionType.User, true));

                // Overwrite permissions
                await context.Client.OverwriteGuildApplicationCommandPermissionsAsync(context.Guild.Id, command.Id, perms);
                
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder.WithContent("User added successfully!"));
            }
            
            [SlashCommand("add_role", "Add permission to role.")]
            public static async Task RoleAdd(InteractionContext context, [Option("role", "Role to be added")] DiscordRole role,
                [ChoiceProvider(typeof(ManagePermissionsChoiceProvider)), Option("command", "The command to which we must give access")] string commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                var command = GetCommand(context, commandName);

                List<DiscordApplicationCommandPermission> perms = new();

                if (command == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                        discordInteractionResponseBuilder.WithContent("Command not found."));
                    return;
                }
                
                try
                {
                    // Get all current command permissions
                    perms = (await context.Client.GetApplicationCommandPermissionAsync(context.Guild.Id, command.Id))
                        .Permissions.ToList();
                }
                catch (NotFoundException)
                {
                    // Ignore if permissions not found
                }
                
                // Add role to the permissions
                perms.Add(new(role.Id, ApplicationCommandPermissionType.Role, true));

                // Overwrite permissions
                await context.Client.OverwriteGuildApplicationCommandPermissionsAsync(context.Guild.Id, command.Id, perms);
                
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder.WithContent("Role added successfully!"));
            }
            
            [SlashCommand("del_user", "Revoke permission from a user.")]
            public static async Task UserDel(InteractionContext context, [Option("user", "User to revoke permission")] DiscordUser user,
                [ChoiceProvider(typeof(ManagePermissionsChoiceProvider)), Option("command", "Command name")] string commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                var command = GetCommand(context, commandName);

                // Get all current command permissions
                var perms = (await context.Client.GetApplicationCommandPermissionAsync(context.Guild.Id, command.Id))
                    .Permissions.ToList();

                var permission = perms.FirstOrDefault(cmd => cmd.Id == user.Id);

                // Remove user from permission list
                perms.Remove(permission);
                
                // Overwrite permissions
                await context.Client.OverwriteGuildApplicationCommandPermissionsAsync(context.Guild.Id, command.Id, perms);
                
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder.WithContent("Permission revoked!"));
            }

            [SlashCommand("del_role", "Revoke permission from a role.")]
            public static async Task UserDel(InteractionContext context, [Option("role", "Role to revoke permission")] DiscordRole role,
                [ChoiceProvider(typeof(ManagePermissionsChoiceProvider)), Option("command", "Command name")] string commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                var command = GetCommand(context, commandName);

                // Get all current command permissions
                var perms = (await context.Client.GetApplicationCommandPermissionAsync(context.Guild.Id, command.Id))
                    .Permissions.ToList();

                var permission = perms.FirstOrDefault(cmd => cmd.Id == role.Id);

                // Remove role from permission list
                perms.Remove(permission);
                
                // Overwrite permissions
                await context.Client.OverwriteGuildApplicationCommandPermissionsAsync(context.Guild.Id, command.Id, perms);
                
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder.WithContent("Permission revoked!"));
            }

            private static DiscordApplicationCommand GetCommand(InteractionContext context, string commandName)
            {
                // Find the command in global and guild commands
                KeyValuePair<ulong?, IReadOnlyList<DiscordApplicationCommand>> appCommands = context.Client.GetApplicationCommands()
                    .RegisteredCommands.FirstOrDefault(cmd =>
                        (cmd.Key == context.Guild.Id || cmd.Key == null) && cmd.Value.Any(x => x.Name == commandName));
                DiscordApplicationCommand command = appCommands.Value.First(cmd => cmd.Name == commandName);

                // Find the command only in guild commands
                // DiscordApplicationCommand command = (await context.Guild.GetApplicationCommandsAsync()).FirstOrDefault(cmd => cmd.Name == commandName);
                return command;
            }
        }
    }
    
    public class ManagePermissionsChoiceProvider : IChoiceProvider
    {
        internal static Dictionary<string, MethodInfo> Commands = new();

        public static void SearchCommands(Type type, string commandName = "")
        {
            // Get all nested group commands in the type variable/class
            IEnumerable<Type> nestedTypes = type.GetNestedTypes().Where(type => type?.GetCustomAttribute<SlashCommandGroupAttribute>() != null);
            // If any nested group commands are available
            if (nestedTypes.Any())
            {
                // Iterate through each subgroup command
                foreach (Type nestedType in nestedTypes)
                {
                    SlashCommandGroupAttribute slashCommandGroupAttribute = nestedType.GetCustomAttribute<SlashCommandGroupAttribute>();
                    // Add the group command to the previous command name. This means it'd look like:
                    // /groupCommand subcommand
                    // instead of:
                    // /subcommand
                    commandName += ' ' + slashCommandGroupAttribute.Name;
                    // There are still nested classes, throw it back into the recursive loop.
                    SearchCommands(nestedType, commandName);
                }
            }
            else
            {
                // Get all slash commands in the class
                IEnumerable<MethodInfo> commands = type.GetMethods().Where(method => method.GetCustomAttribute<SlashCommandAttribute>() != null);
                if (commands.Any())
                {
                    foreach (MethodInfo command in commands)
                    {
                        SlashCommandAttribute slashCommandAttribute = command.GetCustomAttribute<SlashCommandAttribute>();
                        // We assign a temporary variable here, because if we added it to commandName, then our commands would look like:
                        // /groupCommand test
                        // /groupCommand test delete
                        // /groupCommand test delete create
                        // instead of:
                        // /groupCommand test
                        // /groupCommand delete
                        // /groupCommand create
                        string subCommand = commandName + ' ' + slashCommandAttribute.Name;
                        Commands.TryAdd(subCommand.Trim(), command);
                    }
                }
            }
        }

        /// <summary>
        /// Using Reflection, we search our program for ApplicationCommandsModules and register them as commands.
        /// </summary>
        /// <returns>A list of application slash commands.</returns>
        public Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            List<DiscordApplicationCommandOptionChoice> discordApplicationCommandOptionChoices = new();

            // All top level command classes
            IEnumerable<Type> commandClasses = Assembly.GetEntryAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(ApplicationCommandsModule)) && !type.IsNested);

            // Find all command or subgroup commands from the classes
            foreach (Type command in commandClasses)
            {
                SearchCommands(command);
            }

            // SearchCommands registers the commands into a Dictionary<string, MethodInfo>. Since we only need the command name, we can just select the keys.
            foreach (string commandName in Commands.Keys)
            {
                // Create the new choice option: new DiscordApplicationCommandOptionChoice("public name", "value"). Very similar to a dictionary.
                DiscordApplicationCommandOptionChoice discordApplicationCommandOptionChoice = new(commandName, commandName);
                // Add the new option to the other options.
                discordApplicationCommandOptionChoices.Add(discordApplicationCommandOptionChoice);
            }

            // Sort the options alphabetically, in case Discord doesn't do that for us already.
            discordApplicationCommandOptionChoices.Sort((DiscordApplicationCommandOptionChoice x, DiscordApplicationCommandOptionChoice y) => x.Name.CompareTo(y.Name));

            // Return our commands.
            return Task.FromResult(discordApplicationCommandOptionChoices.AsEnumerable());
        }
    }
}