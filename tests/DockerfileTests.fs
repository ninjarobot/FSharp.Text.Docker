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
            """RUN ["apt-get","update"]"""
            """RUN ["apt-get","install","-y","wget"]"""
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

[<Fact>]
let ``RUN exec no args`` () =
    let instruction = Run (Exec ("apt-get", [])) |> printInstruction
    Assert.Equal ("""RUN ["apt-get"]""", instruction)

[<Fact>]
let ``RUN exec with args`` () =
    let instruction = Run (Exec ("apt-get", ["install"; "-y"; "something"])) |> printInstruction
    Assert.Equal ("""RUN ["apt-get","install","-y","something"]""", instruction)

[<Fact>]
let ``RUN exec with quotes in args`` () =
    let instruction = Run (Exec ("apt-get", ["install"; "-y"; """something"quoted with \ slashes / in it"""])) |> printInstruction
    Assert.Equal ("""RUN ["apt-get","install","-y","something\"quoted with \\ slashes \/ in it"]""", instruction)

[<Fact>]
let ``RUN shell command`` () =
    let instruction = Run (ShellCommand ("apt-get install -y wget")) |> printInstruction
    Assert.Equal ("""RUN apt-get install -y wget""", instruction)

[<Fact>]
let ``CMD exec no args`` () =
    let instruction = Cmd (Exec ("mono", [])) |> printInstruction
    Assert.Equal ("""CMD ["mono"]""", instruction)

[<Fact>]
let ``CMD exec with args`` () =
    let instruction = Cmd (Exec ("mono", ["myapp.exe"; "--someoption"])) |> printInstruction
    Assert.Equal ("""CMD ["mono","myapp.exe","--someoption"]""", instruction)

[<Fact>]
let ``CMD exec with quotes in args`` () =
    let instruction = Cmd (Exec ("mono", ["myapp.exe"; """something"quoted with \ slashes / in it"""])) |> printInstruction
    Assert.Equal ("""CMD ["mono","myapp.exe","something\"quoted with \\ slashes \/ in it"]""", instruction)

[<Fact>]
let ``CMD shell command`` () =
    let instruction = Cmd (ShellCommand ("mono myapp.exe --someoption")) |> printInstruction
    Assert.Equal ("CMD mono myapp.exe --someoption", instruction)
