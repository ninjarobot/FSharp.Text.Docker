module DockerfileTests

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

[<Fact>]
let ``FROM with image only`` () =
    let instruction = From ("debian", None, None) |> printInstruction
    Assert.Equal ("FROM debian", instruction)

[<Fact>]
let ``FROM with image and tag`` () =
    let instruction = From ("debian", Some("stretch"), None) |> printInstruction
    Assert.Equal ("FROM debian:stretch", instruction)

[<Fact>]
let ``FROM with named image and tag`` () =
    let instruction = From ("debian", Some("stretch"), Some("deb-stretch")) |> printInstruction
    Assert.Equal ("FROM debian:stretch AS deb-stretch", instruction)
