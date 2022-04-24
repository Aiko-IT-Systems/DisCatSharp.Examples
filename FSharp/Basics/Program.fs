open Basics.Bot

[<EntryPoint>]
let main argv =
    argv
    |> Array.tryHead
    |> Option.iter (Bot.create >> Bot.runAsync >> (fun t -> t.Wait()))
    0 // return an integer exit code
