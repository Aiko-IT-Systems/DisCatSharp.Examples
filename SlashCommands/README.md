# Slash Commands

This example bot shows you how to use the basic usage of slash commands. When learning how to use slash commands, it's intended to look through the files in the following order:
- [Program.cs](./src/Program.cs) (Setting up slash commands)
- [Ping.cs](./src/Commands/Ping.cs) (Basic usage of slash commands)
- [RoleInfo.cs](./src/Commands/RoleInfo.cs) (Slash commands with arguments)
- [RollRandom.cs](./src/Commands/RollRandom.cs) (Slash commands with enums)
- [Slap.cs](./src/Commands/Slap.cs) (Shows how to use DiscordEntities)
- [Tags.cs](./src/Commands/Tags.cs) (Group slash commands + optional arguments)

Throughout the project, you'll occasionally come across "challenge comments" which looks like such:

```cs
// CHALLENGE: Some simple challenge
```

These challenge comments are intended for you to complete on your own. Clone the repo, modify the code and complete those challenges!

To get the bot up and running, run the following command:

```
dotnet run <someBotTokenHere>
```
