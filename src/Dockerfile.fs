namespace FSharp.Text.Docker

#if FABLE
open Fable.Core
open Fable.Import
#endif
module Dockerfile =

    type Command =
        | ShellCommand of string
        | Exec of Executable:string * Arg:string list

    type KeyVal =
        | KeyValuePair of Key:string * Value:string
        | Dictionary of Map<string,string>

    type Source =
        | SingleSource of Source:string
        | MultipleSources of Source:string list

    type BuildStage = 
        | Name of string
        | Index of int
        member this.AsString =
            match this with
            | Name n -> n
            | Index i -> string i

    type Signal =
        | Sigquit
        | Sigint
        | Sigkill
        | Number of uint8

    type Duration = 
        | Seconds of uint32
        | Minutes of uint32

    type HealthcheckOption =
        | Interval of Duration
        | Timeout of Duration
        | StartPeriod of Duration
        | Retries of uint32

    type Healthcheck =
        | HealthcheckCmd of Cmd:Command * HealthcheckOption list
        | HealthcheckNone

    /// Instructions in a Dockerfile
    type Instruction =
        | From of Image:string * Tag:string option * Name:string option
        | Run of Command
        | Cmd of Command
        // Passing args to the entrypoint for the image.
        | CmdArgs of Args:string list
        | Label of KeyVal
        | Expose of Ports:uint16 list
        | Env of KeyVal
        | Add of Source * Destination:string
        | Copy of Source * Destination:string * From:BuildStage option
        | Entrypoint of Command
        | Volume of string list
        | User of Username:string * Group:string option
        | WorkDir of string
        | Arg of Name:string * Default:string option
        | Onbuild of Instruction
        | Stopsignal of Signal
        | Shell of Executable:string * Parameters:string list
        | Healthcheck of Healthcheck

    let private stringsToJsonArray (strings:string list) =
        let arrStr = strings |> Array.ofList
#if FABLE
        JsInterop.toJson arrStr
#else
        let serializeOptions = System.Text.Json.JsonSerializerOptions(Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
        System.Text.Json.JsonSerializer.Serialize (arrStr, serializeOptions)
#endif

    let private stringsToQuotedArray (strings:string list) =
        strings |> List.map (sprintf "\"%s\"") |> String.concat ", " |> sprintf "[%s]"
    
    let private stringContainsWhitepace (s:string) =
        if isNull (s) then
            false
        else
#if FABLE
            let re = JS.RegExp.Create("/\s/")
            re.test(s)
#else
            System.Linq.Enumerable.Any (s, fun c -> System.Char.IsWhiteSpace (c))
#endif

    /// Prints a Docker instruction to a string.
    let rec printInstruction (instruction:Instruction) =
        match instruction with
        | From (img, Some (tag), Some(name)) -> sprintf "FROM %s:%s AS %s" img tag name
        | From (img, None, Some(name)) -> sprintf "FROM %s AS %s" img name
        | From (img, Some(tag), None) -> sprintf "FROM %s:%s" img tag
        | From (img, None, None) -> sprintf "FROM %s" img
        | Run (ShellCommand (command)) -> sprintf "RUN %s" command
        | Run (Exec (executable, args)) -> 
            sprintf "RUN %s" (executable::args |> stringsToJsonArray)
        | Cmd (ShellCommand (command)) -> sprintf "CMD %s" command
        | Cmd (Exec (executable, args)) -> 
            sprintf "CMD %s" (executable::args |> stringsToJsonArray)
        | CmdArgs (args) ->
            sprintf "CMD %s" (args |> stringsToJsonArray)
        | Label (KeyValuePair (key, value)) ->
            sprintf "LABEL %s=%s" key value
        | Label (Dictionary (map)) ->
            map
            |> Seq.map (fun kv -> sprintf "%s=%s" kv.Key kv.Value)
            |> String.concat " "
            |> sprintf "LABEL %s"
        | Expose (ports) ->
            ports
            |> List.map (sprintf "%d")
            |> String.concat " "
            |> sprintf "EXPOSE %s"
        | Env (KeyValuePair (key, value)) ->
            sprintf "ENV %s %s" key value
        | Env (Dictionary (map)) ->
            map
            |> Seq.map (fun kv -> sprintf "%s=%s" kv.Key kv.Value)
            |> String.concat " "
            |> sprintf "ENV %s"
        | Add (SingleSource (source), dest) ->
            if source |> stringContainsWhitepace || dest |> stringContainsWhitepace then
                [source; dest] |> stringsToQuotedArray
                |> sprintf "ADD %s"
            else
                sprintf "ADD %s %s" source dest
        | Add (MultipleSources (sources), dest) ->
            if sources |> List.exists(stringContainsWhitepace) || dest |> stringContainsWhitepace then
                sources@[dest] |> stringsToQuotedArray
                |> sprintf "ADD %s"
            else
                sprintf "ADD %s %s" (sources |> String.concat " ") dest
        | Copy (SingleSource (source), dest, None) ->
            if source |> stringContainsWhitepace || dest |> stringContainsWhitepace then
                [source; dest] |> stringsToQuotedArray
                |> sprintf "COPY %s"
            else
                sprintf "COPY %s %s" source dest
        | Copy (SingleSource (source), dest, Some (from)) ->
            if source |> stringContainsWhitepace || dest |> stringContainsWhitepace then
                [source; dest] |> stringsToQuotedArray
                |> sprintf "COPY --from=%s %s" from.AsString
            else
                sprintf "COPY --from=%s %s %s" from.AsString source dest
        | Copy (MultipleSources (sources), dest, None) ->
            if sources |> List.exists(stringContainsWhitepace) || dest |> stringContainsWhitepace then
                sources@[dest] |> stringsToQuotedArray
                |> sprintf "COPY %s"
            else
                sources@[dest] |> String.concat " "
                |> sprintf "COPY %s"
        | Copy (MultipleSources (sources), dest, Some(from)) ->
            if sources |> List.exists(stringContainsWhitepace) || dest |> stringContainsWhitepace then
                sources@[dest] |> stringsToQuotedArray
                |> sprintf "COPY --from=%s %s" from.AsString
            else
                sources@[dest] |> String.concat " "
                |> sprintf "COPY --from=%s %s" from.AsString
        | Entrypoint (Exec (executable, args)) -> 
            sprintf "ENTRYPOINT %s" (executable::args |> stringsToQuotedArray)
        | Entrypoint (ShellCommand (command)) ->
            sprintf "ENTRYPOINT %s" command
        | Volume (paths) ->
            paths |> stringsToQuotedArray |> sprintf "VOLUME %s"
        | User (username, None) ->
            sprintf "USER %s" username
        | User (username, Some(group)) ->
            sprintf "USER %s:%s" username group
        | WorkDir (path) ->
            sprintf "WORKDIR %s" path
        | Arg (name, None) ->
            sprintf "ARG %s" name
        | Arg (name, Some(dflt)) ->
            sprintf "ARG %s=%s" name dflt
        | Onbuild (instruction) ->
            instruction |> printInstruction |> sprintf "ONBUILD %s"
        | Stopsignal (signal) ->
            match signal with
            | Sigint -> "STOPSIGNAL SIGINT"
            | Sigquit -> "STOPSIGNAL SIGQUIT"
            | Sigkill -> "STOPSIGNAL SIGKILL"
            | Number n -> sprintf "STOPSIGNAL %i" n
        | Healthcheck ( HealthcheckCmd (command, options)) ->
            let printDuration duration = 
                match duration with
                | Seconds n -> sprintf "%is" n
                | Minutes n -> sprintf "%im" n
            let allOptions =
                options |> List.map (fun opt ->
                    match opt with
                    | Interval (duration) -> duration |> printDuration |> sprintf "--interval=%s "
                    | Timeout (duration) -> duration |> printDuration |> sprintf "--timeout=%s "
                    | StartPeriod (duration) -> duration |> printDuration |> sprintf "--start-period=%s "
                    | Retries (quantity) -> sprintf "--retries=%i " quantity
                )
                |> String.concat ""
            let cmdInstruction = command |> Cmd |> printInstruction
            sprintf "HEALTHCHECK %s%s" allOptions cmdInstruction
        | Healthcheck (HealthcheckNone) ->
            "HEALTHCHECK NONE"
        | Shell (executable, parameters) ->
            executable::parameters |> stringsToQuotedArray
            |> sprintf "SHELL %s"
        
    /// Concatenates a list of Dockerfile instructions into a single Dockerfile
    let buildDockerfile (instructions:Instruction list) =
        instructions |> List.map (printInstruction) |> String.concat System.Environment.NewLine

module Builders =
    type DockerfileSpec =
        { Instructions : Dockerfile.Instruction list }
        member this.Build () = Dockerfile.buildDockerfile this.Instructions
        member this.Stage : string option =
            let fromAs = function
                | Dockerfile.From (_, _, Some buildStage) -> Some buildStage
                | _ -> None
            this.Instructions |> List.choose fromAs |> List.tryHead
            
    type DockerfileBuilder () =
        member _.Bind (config:DockerfileSpec, fn) : DockerfileSpec = fn config
        member _.Combine (a:DockerfileSpec, b:DockerfileSpec) = { Instructions = a.Instructions @ b.Instructions }
        member _.Delay (fn:unit -> DockerfileSpec) = fn ()
        member _.Yield _ = { Instructions = [] }
        member _.YieldFrom (config:DockerfileSpec) = config
        member _.Zero _ = { Instructions = [] }
        [<CustomOperation "from">]
        member _.From (config:DockerfileSpec, baseImage:string) =
            let instruction =
                match baseImage.Split [|':'|] with
                | [| name |] -> Dockerfile.From(name, None, None)
                | [| name; version |] -> Dockerfile.From(name, Some version, None)
                | _ -> invalidArg "baseImage" "Image should be of form 'name:version'"
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "from_stage">]
        member _.FromStage (config:DockerfileSpec, baseImage:string, stage:string) =
            let instruction =
                match baseImage.Split [|':'|] with
                | [| name |] -> Dockerfile.From(name, None, Some stage)
                | [| name; version |] -> Dockerfile.From(name, Some version, Some stage)
                | _ -> invalidArg "baseImage" "Image should be of form 'name:version'"
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "cmd">]
        member _.CmdShellCommand (config:DockerfileSpec, shellCmd:string) =
            let instruction = Dockerfile.Cmd(Dockerfile.ShellCommand shellCmd)
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "entrypoint">]
        member _.Entrypoint(config: DockerfileSpec, shellCmd: string) =
            let instruction = Dockerfile.Entrypoint(Dockerfile.ShellCommand shellCmd)
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "copy">]
        member _.Copy (config:DockerfileSpec, source:string, dest:string) =
            let instruction = Dockerfile.Copy(Dockerfile.SingleSource source, dest, None)
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "copy_from">]
        member _.CopyFrom (config:DockerfileSpec, stage:string, source:string, dest:string) =
            let instruction = Dockerfile.Copy(Dockerfile.SingleSource source, dest, Some (Dockerfile.BuildStage.Name stage))
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "env">]
        member _.Env (config:DockerfileSpec, envVar:string * string) =
            let instruction = Dockerfile.Env (Dockerfile.Dictionary ([envVar] |> Map.ofList))
            { config with Instructions = config.Instructions @ [ instruction ] }
        member _.Env (config:DockerfileSpec, envVars:Map<string, string>) =
            let instruction = Dockerfile.Env (Dockerfile.KeyVal.Dictionary envVars)
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "env_vars">]
        member _.EnvVars (config:DockerfileSpec, envVars:(string * string) list) =
            let instruction = Dockerfile.Env (Dockerfile.KeyVal.Dictionary (envVars |> Map.ofList))
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "expose">]
        member _.Expose (config:DockerfileSpec, port:int) =
            let instruction = Dockerfile.Expose [(uint16 port)]
            { config with Instructions = config.Instructions @ [ instruction ] }
        member _.Expose (config:DockerfileSpec, ports:int list) =
            let instruction = Dockerfile.Expose (ports |> List.map uint16)
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "run_exec">]
        member _.RunExec (config:DockerfileSpec, exec:string, args:string list) =
            let instruction = Dockerfile.Run(Dockerfile.Exec (exec, args))
            { config with Instructions = config.Instructions @ [ instruction ] }
        member _.RunExec (config:DockerfileSpec, exec:string, args:string) =
            let instruction = Dockerfile.Run(Dockerfile.Exec (exec, List.ofArray (args.Split null)))
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "run">]
        member _.RunShell (config:DockerfileSpec, shellCmd:string) =
            let instruction = Dockerfile.Run(Dockerfile.ShellCommand shellCmd)
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "user">]
        member _.User (config:DockerfileSpec, user:string) =
            let instruction =
                match user.Split [|':'|] with
                | [| username |] -> Dockerfile.User(username, None)
                | [| username; group |] -> Dockerfile.User(username, Some group)
                | _ -> invalidArg "user" "User should be of form 'username:group'"
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "volume">]
        member _.Volume (config:DockerfileSpec, volume:string) =
            let instruction = Dockerfile.Volume [volume]
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "add_volumes">]
        member _.AddVolumes (config:DockerfileSpec, volumes:string list) =
            let instruction = Dockerfile.Volume volumes
            { config with Instructions = config.Instructions @ [ instruction ] }
        [<CustomOperation "workdir">]
        member _.Workdir (config:DockerfileSpec, workdir:string) =
            let instruction = Dockerfile.WorkDir workdir
            { config with Instructions = config.Instructions @ [ instruction ] }
    let dockerfile = DockerfileBuilder ()
