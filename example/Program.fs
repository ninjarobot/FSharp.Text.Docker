namespace FSharp.Text.Docker.Example

open System
open FSharp.Text.Docker
open FSharp.Text.Docker.Dockerfile


module Program =

    let shortExample = 
        [
            From ("debian", Some("stretch-slim"), None)
            Run (Exec ("apt-get", ["update"]))
            Run (Exec ("apt-get", ["install"; "-y"; "wget"]))
            Env (KeyVal.KeyValuePair("LANG", "C.UTF-8"))
            Copy (SingleSource("."), "/app", None)
            WorkDir ("/app")
            Cmd (ShellCommand ("wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz"))
        ]
        |> Dockerfile.buildDockerfile
        |> printfn "#### Example Dockerfile #### \n %A \n"

    let quickPaketDockerfile = 
        let multipleShellCommands = 
            [
                "apt-get update"
                "apt-get install -y --no-install-recommends wget"
                "wget https://github.com/fsprojects/Paket/releases/download/5.95.0/paket.exe"
                "chmod a+r paket.exe && mv paket.exe /usr/local/lib/"
                """printf '#!/bin/sh\nexec /usr/bin/mono /usr/local/lib/paket.exe "$@"' >> /usr/local/bin/paket"""
                "chmod u+x /usr/local/bin/paket"
            ]
            |> String.concat " \ \n    && "
        [
            From ("fsharp", Some("4.1.25"), None)
            Run (ShellCommand (multipleShellCommands))
            Entrypoint (Exec ("paket", []))
        ] |> Dockerfile.buildDockerfile
        |> printfn "#### Paket Dockerfile ##### \n %A \n"

    [<EntryPoint>]
    let main argv =
        shortExample
        quickPaketDockerfile
        0 // return an integer exit code
