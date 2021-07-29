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
