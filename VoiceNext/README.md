# Voice Example

This sample still lives in the `VoiceNext` folder, but the package reference now targets the modern `DisCatSharp.Voice` package introduced in the 10.7.0 line.

## What this sample covers

- Joining and leaving voice channels with `/connect` and `/leave`
- Playing local audio files with `/play`, `/pause`, `/resume`, and `/stop`
- Receiving and recording incoming voice into one timeline-aware WAV file per speaker with `/record_start`, `/record_status`, and `/record_stop`
- Challenge-driven extension points in code comments so you can grow the sample into a moderation, podcast, or transcription workflow

## Quick start

Run the bot with a token as the first argument:

```bash
dotnet run <someBotTokenHere>
```

You can also supply the token through the `DISCATSHARP_TOKEN` or `DISCORD_TOKEN` environment variable.

## Recommended recording walkthrough

1. Join a voice channel with the account that will control the bot.
2. Run `/connect` so the bot joins your channel.
3. Run `/record_start` to arm the inbound voice capture flow.
4. Let one or more people talk for a few seconds.
5. Run `/record_status` if you want a live checkpoint card while testing packet loss or silence trimming ideas.
6. Run `/record_stop` to upload the captured WAV files back to Discord.
7. Run `/leave` after you are done with playback or recording.

## Notes

- `Program.cs` enables inbound voice by calling `UseVoice(new VoiceConfiguration { EnableIncoming = true })`.
- `/record_stop` uploads up to 10 WAV attachments in one Discord message. The code keeps the files per-speaker on purpose so the flow stays easy to inspect.
- The per-speaker outputs are timeline-aware: the sample inserts silence for receive gaps and pads tracks to the shared session length so they can be aligned later in a DAW or editor.
- Timing is anchored to when decoded frames reached the bot, which is editor-friendly but not guaranteed to be sample-accurate under network jitter or dropped packets.
- `/play` still depends on `ffmpeg` being installed and available on your `PATH`.
- The code includes `// CHALLENGE:` comments around permission checks, silence trimming, packet-loss diagnostics, and post-processing handoff ideas.
