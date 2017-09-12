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

[<Fact>]
let ``CMD args only`` () =
    let instruction = CmdArgs (["arg1";"arg2";"arg3"]) |> printInstruction
    Assert.Equal ("""CMD ["arg1","arg2","arg3"]""", instruction)

[<Fact>]
let ``LABEL single item`` () =
    let instruction = Label (KeyValuePair("foo","bar")) |> printInstruction
    Assert.Equal ("LABEL foo=bar", instruction)

[<Fact>]
let ``LABEL from dictionary`` () =
    let instruction = Label (Dictionary (["foo","bar";"abc","def";"item1","testing"] |> Map.ofList)) |> printInstruction
    Assert.Equal ("LABEL abc=def foo=bar item1=testing", instruction)

[<Fact>]
let ``EXPOSE single port`` () =
    let instruction = Expose ([8080us]) |> printInstruction
    Assert.Equal ("EXPOSE 8080", instruction)

[<Fact>]
let ``EXPOSE multiple ports`` () =
    let instruction = Expose ([8080us; 8081us; 8443us]) |> printInstruction
    Assert.Equal ("EXPOSE 8080 8081 8443", instruction)

[<Fact>]
let ``ENV single item`` () =
    let instruction = Env (KeyValuePair ("foo","bar")) |> printInstruction
    Assert.Equal ("ENV foo bar", instruction)

[<Fact>]
let ``ENV from dictionary`` () =
    let instruction = Env (Dictionary (["foo","bar";"abc","def";"item1","testing"] |> Map.ofList)) |> printInstruction
    Assert.Equal ("ENV abc=def foo=bar item1=testing", instruction)

[<Fact>]
let ``ADD single source no whitespace`` () =
    let instruction = Add (SingleSource ("path/to/source", "/dest/in/image")) |> printInstruction
    Assert.Equal ("ADD path/to/source /dest/in/image", instruction)

[<Fact>]
let ``ADD single source with whitespace`` () =
    let instruction = Add (SingleSource ("path to/source", "/dest/in/image")) |> printInstruction
    Assert.Equal ("""ADD ["path to/source", "/dest/in/image"]""", instruction)

[<Fact>]
let ``ADD single source to destination with whitespace`` () =
    let instruction = Add (SingleSource ("path/to/source", "/dest/in image")) |> printInstruction
    Assert.Equal ("""ADD ["path/to/source", "/dest/in image"]""", instruction)

[<Fact>]
let ``ADD single source with whitepace to destination with whitespace`` () =
    let instruction = Add (SingleSource ("path to source", "/dest in image")) |> printInstruction
    Assert.Equal ("""ADD ["path to source", "/dest in image"]""", instruction)
