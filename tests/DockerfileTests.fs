module DockerfileTests

open System
open Xunit
open FSharp.Text.Docker
open FSharp.Text.Docker.Dockerfile

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
    Assert.Equal ("""RUN ["apt-get","install","-y","something\"quoted with \\ slashes / in it"]""", instruction)

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
    Assert.Equal ("""CMD ["mono","myapp.exe","something\"quoted with \\ slashes / in it"]""", instruction)

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
    let instruction = Add (SingleSource ("path/to/source"), "/dest/in/image") |> printInstruction
    Assert.Equal ("ADD path/to/source /dest/in/image", instruction)

[<Fact>]
let ``ADD multiple sources no whitespace`` () =
    let instruction = Add (MultipleSources (["path/to/source"; "some/other/source"]), "/dest/in/image") |> printInstruction
    Assert.Equal ("ADD path/to/source some/other/source /dest/in/image", instruction)

[<Fact>]
let ``ADD single source with whitespace`` () =
    let instruction = Add (SingleSource ("path to/source"), "/dest/in/image") |> printInstruction
    Assert.Equal ("""ADD ["path to/source", "/dest/in/image"]""", instruction)

[<Fact>]
let ``ADD multiple sources with whitespace`` () =
    let instruction = Add (MultipleSources (["path to/source"; "some/other source"]), "/dest/in/image") |> printInstruction
    Assert.Equal ("""ADD ["path to/source", "some/other source", "/dest/in/image"]""", instruction)

[<Fact>]
let ``ADD single source to destination with whitespace`` () =
    let instruction = Add (SingleSource ("path/to/source"), "/dest/in image") |> printInstruction
    Assert.Equal ("""ADD ["path/to/source", "/dest/in image"]""", instruction)

[<Fact>]
let ``ADD single source with whitepace to destination with whitespace`` () =
    let instruction = Add (SingleSource ("path to source"), "/dest in image") |> printInstruction
    Assert.Equal ("""ADD ["path to source", "/dest in image"]""", instruction)

[<Fact>]
let ``COPY single source no whitespace`` () =
    let instruction = Copy (SingleSource ("path/to/source"), "/dest/in/image", None) |> printInstruction
    Assert.Equal ("COPY path/to/source /dest/in/image", instruction)

[<Fact>]
let ``COPY multiple sources no whitespace`` () =
    let instruction = Copy (MultipleSources (["path/to/source"; "some/other/source"]), "/dest/in/image", None) |> printInstruction
    Assert.Equal ("COPY path/to/source some/other/source /dest/in/image", instruction)

[<Fact>]
let ``COPY single source with whitespace`` () =
    let instruction = Copy (SingleSource ("path to/source"), "/dest/in/image", None) |> printInstruction
    Assert.Equal ("""COPY ["path to/source", "/dest/in/image"]""", instruction)

[<Fact>]
let ``COPY multiple sources with whitespace`` () =
    let instruction = Copy (MultipleSources (["path to/source"; "some/other source"]), "/dest/in/image", None) |> printInstruction
    Assert.Equal ("""COPY ["path to/source", "some/other source", "/dest/in/image"]""", instruction)

[<Fact>]
let ``COPY from named layer`` () =
    let instruction = Copy (SingleSource ("path/to/source"), "/dest/in/image", Some (BuildStage.Name "builder")) |> printInstruction
    Assert.Equal ("COPY --from=builder path/to/source /dest/in/image", instruction)

[<Fact>]
let ``ENTRYPOINT exec no args`` () =
    let instruction = Entrypoint (Exec ("/bin/bash", [])) |> printInstruction
    Assert.Equal ("""ENTRYPOINT ["/bin/bash"]""", instruction)

[<Fact>]
let ``ENTRYPOINT exec with args`` () =
    let instruction = Entrypoint (Exec ("bash", ["-c"; "ls"])) |> printInstruction
    Assert.Equal ("""ENTRYPOINT ["bash", "-c", "ls"]""", instruction)

[<Fact>]
let ``ENTRYPOINT exec with quotes in args`` () =
    let instruction = Entrypoint (Exec ("bash", ["-c"; "'echo \"hello world\"'"])) |> printInstruction
    Assert.Equal ("""ENTRYPOINT ["bash", "-c", "'echo "hello world"'"]""", instruction)

[<Fact>]
let ``ENTRYPOINT shell command`` () =
    let instruction = Entrypoint (ShellCommand ("bash -c ls")) |> printInstruction
    Assert.Equal ("""ENTRYPOINT bash -c ls""", instruction)

[<Fact>]
let ``VOLUME single path`` () =
    let instruction = Volume (["test"]) |> printInstruction
    Assert.Equal ("""VOLUME ["test"]""", instruction)

[<Fact>]
let ``USER no group`` () =
    let instruction = User ("foo", None) |> printInstruction
    Assert.Equal ("USER foo", instruction)

[<Fact>]
let ``USER with group`` () =
    let instruction = User ("foo", Some("bar")) |> printInstruction
    Assert.Equal ("USER foo:bar", instruction)

[<Fact>]
let ``WORKDIR with path`` () =
    let instruction = WorkDir ("path/to/dir") |> printInstruction
    Assert.Equal ("WORKDIR path/to/dir", instruction)

[<Fact>]
let ``ARG with default`` () =
    let instruction = Arg ("user1", Some ("someuser")) |> printInstruction
    Assert.Equal ("ARG user1=someuser", instruction)

[<Fact>]
let ``ARG without default`` () =
    let instruction = Arg ("buildno", None) |> printInstruction
    Assert.Equal ("ARG buildno", instruction)

[<Fact>]
let ``ONBUILD with ADD`` () =
    let instruction = Onbuild (Add (SingleSource ("."), "/app/src" ) ) |> printInstruction
    Assert.Equal ("ONBUILD ADD . /app/src", instruction)

[<Fact>]
let ``STOPSIGNAL with named signal`` () =
    let instruction = Stopsignal (Sigkill) |> printInstruction
    Assert.Equal ("STOPSIGNAL SIGKILL", instruction)

[<Fact>]
let ``STOPSIGNAL with numeric`` () =
    let instruction = Stopsignal (Number (9uy)) |> printInstruction
    Assert.Equal ("STOPSIGNAL 9", instruction)

[<Fact>]
let ``HEALTHCHECK without options`` () =
    let instruction = Healthcheck (HealthcheckCmd (ShellCommand ("ping -c 2 8.8.8.8"), [])) |> printInstruction
    Assert.Equal ("HEALTHCHECK CMD ping -c 2 8.8.8.8", instruction)

[<Fact>]
let ``HEALTHCHECK with one option`` () =
    let instruction = Healthcheck (HealthcheckCmd (ShellCommand ("ping -c 2 8.8.8.8"), [1u |> Minutes |> Interval])) |> printInstruction
    Assert.Equal ("HEALTHCHECK --interval=1m CMD ping -c 2 8.8.8.8", instruction)

[<Fact>]
let ``HEALTHCHECK with multiple options`` () =
    let options =
        [
            2u |> Seconds |> Timeout
            45u |> Seconds |> StartPeriod
        ]
    let instruction = Healthcheck (HealthcheckCmd (ShellCommand ("ping -c 2 8.8.8.8"), options)) |> printInstruction
    Assert.Equal ("HEALTHCHECK --timeout=2s --start-period=45s CMD ping -c 2 8.8.8.8", instruction)

[<Fact>]
let ``SHELL with args`` () =
    let instruction = Shell ("/bin/sh", ["-c"]) |> printInstruction
    Assert.Equal ("""SHELL ["/bin/sh", "-c"]""", instruction)
