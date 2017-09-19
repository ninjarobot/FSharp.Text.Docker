open System
open FSharp.Text.Docker
open FSharp.Text.Docker.Dockerfile

[<EntryPoint>]
let main argv =
    [
        From ("hello-world", None, None)
        Expose ([80us; 443us])
        User ("dave", Some ("greeters"))
    ]
    |> Dockerfile.buildDockerfile
    |> printfn "#### Hello world dockerfile ####%s%s" Environment.NewLine
    0 // return an integer exit code
