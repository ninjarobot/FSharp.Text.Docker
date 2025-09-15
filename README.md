FSharp.Text.Docker
========

Interact with docker with the type safety of the F# language.

[![Build and Test](https://github.com/ninjarobot/FSharp.Text.Docker/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/ninjarobot/FSharp.Text.Docker/actions/workflows/build-and-test.yml)
[![FSharp.Text.Docker on Nuget](https://img.shields.io/nuget/v/FSharp.Text.Docker)](https://www.nuget.org/packages/FSharp.Text.Docker/)

### Define a dockerfile

A dockerfile can be built using a set of types for all of the supported
instructions, providing type safety and compile time checking when building
a Dockerfile.

A list of instructions is passed to the `buildDockerfile` command, which will
return a string representation of that Dockerfile.

```fsharp
#r "nuget: FSharp.Text.Docker"
open FSharp.Text.Docker.Builders

let dockerSpecBuilder = dockerfile {
    from_stage "mcr.microsoft.com/dotnet/sdk:5.0.302" "builder"
    run_exec "apt-get" "install -y wget"
    run "dotnet new console -lang F# -n foo"
    workdir "foo"
    run "dotnet build -c Release -o app"
    from "mcr.microsoft.com/dotnet/runtime:5.0.8"
    expose 80
    copy_from "builder" "/path/to/source/myApp.dll" "/path/to/dest"
    cmd "dotnet /path/to/dest/myApp.dll"
}
dockerSpecBuilder.Build() |> System.Console.WriteLine
```

The output can be saved as a Dockerfile like the one below.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:5.0.302 AS builder
RUN ["apt-get","install","-y","wget"]
RUN dotnet new console -lang F# -n foo
WORKDIR foo
RUN dotnet build -c Release -o app
FROM mcr.microsoft.com/dotnet/runtime:5.0.8
EXPOSE 80
COPY --from=builder /path/to/source
```

With the full F# language, it becomes relatively simple to use complex logic
in building the Dockerfile.  For example, this snippet will concatenate a set
of shell commands into a single RUN instruction to reduce image layers.

```fsharp
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
|> printfn "%s"
```

The resulting output is a dockerfile as follows, useful for a small image to
execute paket:

```dockerfile
FROM fsharp:4.1.25
RUN apt-get update \ 
    && apt-get install -y --no-install-recommends wget \ 
    && wget https://github.com/fsprojects/Paket/releases/download/5.95.0/paket.exe \ 
    && chmod a+r paket.exe && mv paket.exe /usr/local/lib/ \ 
    && printf '#!/bin/sh\nexec /usr/bin/mono /usr/local/lib/paket.exe "$@"' >> /usr/local/bin/paket \ 
    && chmod u+x /usr/local/bin/paket
ENTRYPOINT ["paket"]
```

### More to come

* Build images (not yet)
* Run containers (not yet)

