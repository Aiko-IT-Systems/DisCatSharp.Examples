using System.Collections.Generic;
using System.Linq;
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
                [Option("command", "The command to which we must give access")] CommandNames commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                DiscordApplicationCommand command = GetCommand(context, commandName.ToString());

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
                [Option("command", "The command to which we must give access")] CommandNames commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                var command = GetCommand(context, commandName.ToString());

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
                
                // Add role to the permissions
                perms.Add(new(role.Id, ApplicationCommandPermissionType.Role, true));

                // Overwrite permissions
                await context.Client.OverwriteGuildApplicationCommandPermissionsAsync(context.Guild.Id, command.Id, perms);
                
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder.WithContent("Role added successfully!"));
            }
            
            [SlashCommand("del_user", "Revoke permission from a user.")]
            public static async Task UserDel(InteractionContext context, [Option("user", "User to revoke permission")] DiscordUser user,
                [Option("command", "Command name")] CommandNames commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                var command = GetCommand(context, commandName.ToString());

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
                [Option("command", "Command name")] CommandNames commandName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new() {IsEphemeral = true};

                var command = GetCommand(context, commandName.ToString());

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

        public enum CommandNames
        {
            slap,
            manage_permissions
        }
    }
}