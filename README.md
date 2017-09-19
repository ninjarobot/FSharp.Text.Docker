FSharp.Text.Docker
========

Interact with docker with the type safety of the F# language.

### Define a dockerfile

A dockerfile can be built using a set of types for all of the supported
instructions, providing type safety and compile time checking when building
a Dockerfile.

A list of instructions is passed to the `buildDockerfile` command, which will
return a string representation of that Dockerfile.

```fsharp
[
    From ("debian", Some("stretch-slim"), None)
    Run (Exec ("apt-get", ["update"]))
    Run (Exec ("apt-get", ["install"; "-y"; "wget"]))
    Env (KeyValuePair("LANG", "C.UTF-8"))
    Copy (SingleSource("."), "/app", None)
    WorkDir ("/app")
    Cmd (ShellCommand ("wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz"))
]
|> Dockerfile.buildDockerfile
```

The output can be saved as a Dockerfile like the one below.

```dockerfile
FROM debian:stretch-slim
RUN ["apt-get","update"]
RUN ["apt-get","install","-y","wget"]
ENV LANG C.UTF-8
COPY . /app
WORKDIR /app
CMD wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz
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

