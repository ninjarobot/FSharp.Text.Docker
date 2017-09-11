module Tests

open System
open Xunit
open FSharp.Docker
open FSharp.Docker.Dockerfile

[<Fact>]
let ``Build a dockerfile`` () =
    let dockerfile =
        [
            From ("debian", Some("stretch-slim"), None)
            Run (Exec ("apt-get", ["update"]))
            Run (Exec ("apt-get", ["install"; "-y"; "wget"]))
            Cmd (ShellCommand ("wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz"))
        ]
        |> Dockerfile.buildDockerfile
    let expected =
        [
            """FROM debian:stretch-slim"""
            """RUN ["apt-get", "update"]"""
            """RUN ["apt-get", "install", "-y", "wget"]"""
            """CMD wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz"""
        ]
    let dockerfileLines = dockerfile.Split(Environment.NewLine)
    expected |> List.iteri (fun idx line -> Assert.Equal (line, dockerfileLines.[idx]))
