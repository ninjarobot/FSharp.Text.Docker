namespace FSharp.Docker.Example

open System
open FSharp.Docker
open FSharp.Docker.Dockerfile


module Program =

    [<EntryPoint>]
    let main argv =
        [
            From ("debian", Some("stretch-slim"), None)
            Run (Exec ("apt-get", ["update"]))
            Run (Exec ("apt-get", ["install"; "-y"; "wget"]))
            Cmd (ShellCommand ("wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz"))
        ]
        |> Dockerfile.buildDockerfile
        |> printfn "%A"
        0 // return an integer exit code
