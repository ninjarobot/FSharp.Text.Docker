module BuilderTests

open System
open Xunit
open FSharp.Text.Docker.Builders

[<Fact>]
let ``Simple builder`` () =
    let myDockerfile = dockerfile {
        from_stage "mcr.microsoft.com/dotnet/sdk:5.0.302" "builder"
        run_exec "apt-get" "install -y wget"
        run "dotnet new console -lang F# -n foo"
        workdir "foo"
        run "dotnet build -c Release -o app"
        from "mcr.microsoft.com/dotnet/runtime:5.0.8"
        expose 80
        copy_from "builder" "/path/to/source/myApp.dll" "/path/to/dest"
        entrypoint "dotnet" []
        cmd "/path/to/dest/myApp.dll"
    }
    let spec = myDockerfile.Build ()
    let expected = """
FROM mcr.microsoft.com/dotnet/sdk:5.0.302 AS builder
RUN ["apt-get","install","-y","wget"]
RUN dotnet new console -lang F# -n foo
WORKDIR foo
RUN dotnet build -c Release -o app
FROM mcr.microsoft.com/dotnet/runtime:5.0.8
EXPOSE 80
COPY --from=builder /path/to/source/myApp.dll /path/to/dest
ENTRYPOINT ["dotnet"]
CMD /path/to/dest/myApp.dll
"""
    Assert.Equal (expected.Trim(), spec)

[<Fact>]
let ``From parsing`` () =
    let myDockerfile = dockerfile {
        from "good"
    }
    let spec = myDockerfile.Build ()
    Assert.Equal ("FROM good", spec)
    
    let myDockerfile = dockerfile {
        from "good:tag"
    }
    let spec = myDockerfile.Build ()
    Assert.Equal ("FROM good:tag", spec)
    
    Assert.Throws<ArgumentException> (fun _ ->
        let _ = dockerfile {
            from "bad:image:tag"
        }
        ()
    ) |> ignore

[<Fact>]
let ``User parsing`` () =
    let myDockerfile = dockerfile {
        user "someuser"
    }
    let spec = myDockerfile.Build ()
    Assert.Equal ("USER someuser", spec)

    let myDockerfile = dockerfile {
        user "someuser:wheel"
    }
    let spec = myDockerfile.Build ()
    Assert.Equal ("USER someuser:wheel", spec)
    
    Assert.Throws<ArgumentException> (fun _ ->
        let _ = dockerfile {
            from "someuser:wheel:WAT"
        }
        ()
    ) |> ignore

[<Fact>]
let ``Dockerfile composed of multiple builders`` () =
    let myDockerfile = dockerfile {
        let! builder = dockerfile {
            from_stage "mcr.microsoft.com/dotnet/sdk:5.0.302" "builder"
            run_exec "apt-get" "install -y wget"
            run "dotnet new console -lang F# -n foo"
            workdir "foo"
            run "dotnet build -c Release -o app"
        }
        yield! builder
        match builder.Stage with
        | Some stage ->
            yield! dockerfile {
                from "mcr.microsoft.com/dotnet/runtime:5.0.8"
                expose 80
                env ("DOTNET_EnableDiagnostics", "0")
                env_vars [
                    "FOO", "BAR"
                    "NETCORE", "5.0.8"
                ]
                copy_from stage "/path/to/source/myApp.dll" "/path/to/dest"
            }
        | _ -> failwith "Missing stage in 'builder' dockerfile"
        yield! dockerfile { cmd "dotnet /path/to/dest/myApp.dll" }
    }
    let spec = myDockerfile.Build ()
    let expected = """
FROM mcr.microsoft.com/dotnet/sdk:5.0.302 AS builder
RUN ["apt-get","install","-y","wget"]
RUN dotnet new console -lang F# -n foo
WORKDIR foo
RUN dotnet build -c Release -o app
FROM mcr.microsoft.com/dotnet/runtime:5.0.8
EXPOSE 80
ENV DOTNET_EnableDiagnostics=0
ENV FOO=BAR NETCORE=5.0.8
COPY --from=builder /path/to/source/myApp.dll /path/to/dest
CMD dotnet /path/to/dest/myApp.dll
"""
    Assert.Equal (expected.Trim(), spec)
