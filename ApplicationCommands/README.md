# Application Commands

This example bot shows you how to use the basic usage of application commands. When learning how to use app commands, it's intended to look through the files in the following order:
- [Program.cs](./src/Program.cs) (Setting up application commands)
- [Ping.cs](./src/Commands/Ping.cs) (Basic usage of slash commands)
- [MessageCopy.cs](./src/Commands/MessageCopy.cs) (Basic usage of message context menu command)
- [UserInfo.cs](./src/Commands/UserInfo.cs) (Basic usage of user context menu command)
- [RoleInfo.cs](./src/Commands/RoleInfo.cs) (Slash commands with arguments)
- [RollRandom.cs](./src/Commands/RollRandom.cs) (Slash commands with enums)
- [Slap.cs](./src/Commands/Slap.cs) (Shows how to use DiscordEntities + permissions)
- [Tags.cs](./src/Commands/Tags.cs) (Group slash commands + optional arguments)
- [Tell.cs](./src/Commands/Tell.cs) (Shows usage of the ChoiceAttribute)
- [TriggerHelp.cs](./src/Commands/TriggerHelp.cs) (Shows advanced usage of ChoiceProvider attribute with Reflection)
- [ManagePermissions.cs](./src/Commands/ManagePermissions.cs) (Shows how to change command permissions after they are registered)

Throughout the project, you'll occasionally come across "challenge comments" which looks like such:

```cs
// CHALLENGE: Some simple challenge
```

These challenge comments are intended for you to complete on your own. Clone the repo, modify the code and complete those challenges!

*You must enter a token as the first argument. As the second argument, you can specify the guild ID, otherwise the commands will be created globally. The third argument is the role to which you want to issue the slap command (currently the ability to create global commands with permissions in development).*

To get the bot up and running, run the following command:

```
dotnet run <someBotTokenHere> [guildId] [slapRoleId]
```
