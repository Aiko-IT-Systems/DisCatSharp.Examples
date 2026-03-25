# DisCatSharp.Examples
Example Discord Bots written in C# with DisCatSharp.

Includes examples like music bots supporting app commands and has some "challenges" for you to work on.

You need a compatible IDE to work with it, and .NET 9 SDK.


### Examples
| **Example**             | **Up-to-date** | **Description**                                                                                   |
|-------------------------|----------------|---------------------------------------------------------------------------------------------------|
| **Basics**              | Yes            | Bot initialization plus approachable command and slash-command flows such as reminders, avatar media-gallery previews, and Components V2-first teaching surfaces with room for challenge-driven extensions. |
| **ApplicationCommands** | Yes            | Create and use application commands: registration, deferred responses, Components V2 cards, autocomplete, DI-backed tags, role/help cards, and other practical command patterns. |
| **Interactivity**       | Yes            | Interactivity, components (buttons, select menus), threads, stages, owner-scoped panels, and a practical multi-step task workflow with modal capture and follow-up handling. |
| **VoiceNext**           | No             | Play local audio files in voice channels with the modern `DisCatSharp.Voice` package.             |
| **Lavalink**            | No             | Play audio from YouTube in voice channels.                                                        |
| **Hosting**             | Yes            | Initialize a bot as a service and surface host-backed runtime status through Components V2 cards. |

### Components V2 feature map
| **Example / file** | **Uses `WithV2Components()`** | **Containers** | **Sections** | **Media gallery** | **Modals** | **Notes** |
|---|---|---|---|---|---|---|
| `Basics\Commands\Main.cs` | Yes | Yes | No | No | No | Text-command outputs rendered as Components V2 cards. |
| `Basics\AppCommands\Main.cs` | Yes | Yes | Yes | Yes | No | Slash-command avatar/profile flows now include a Components V2 media gallery. |
| `ApplicationCommands\src\Commands\Ping.cs` | Yes | Yes | No | No | No | Minimal deferred-response Components V2 example. |
| `ApplicationCommands\src\Commands\RollRandom.cs` | Yes | Yes | No | No | No | Deferred DI-backed result card. |
| `ApplicationCommands\src\Commands\Tags.cs` | Yes | Yes | No | No | No | Stateful tag flows with autocomplete and Components V2 cards. |
| `ApplicationCommands\src\Commands\RoleInfo.cs` | Yes | Yes | Yes | No | No | Role information card built from containers plus sections. |
| `ApplicationCommands\src\Commands\TriggerHelp.cs` | Yes | Yes | Yes | No | No | Reflection-driven help card rendered with sections. |
| `Hosting\Commands\TestCommands.cs` | Yes | Yes | No | No | No | Host-backed runtime status card for text commands. |
| `Hosting\Commands\AppCommands.cs` | Yes | Yes | No | No | No | Host-backed runtime status card for slash commands. |
| `Interactivity\Commands\InteractivityCommands.cs` | Yes | Yes | No | No | Yes | Message, reaction, button, select, and workflow examples; `/workflow` now captures details via modal before follow-up buttons. |
| `Interactivity\Program.cs` | Yes | Yes | No | No | No | Owner-scoped randomizer edits persistent Components V2 panels. |
