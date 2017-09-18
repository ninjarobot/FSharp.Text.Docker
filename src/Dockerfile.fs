namespace FSharp.Docker

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

    type Signal =
        | Sigquit
        | Sigint
        | Sigkill
        | Number of uint8

    type Healthcheck =
        | HealthcheckCmd of Cmd:string * Interval:uint32 option * Timeout:uint32 option * StartPertion:uint32 option * Retries:uint32 option
        | HealthcheckNone

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
        use ms = new System.IO.MemoryStream ()
        let arrStr = strings |> Array.ofList
        let serializer = System.Runtime.Serialization.Json.DataContractJsonSerializer (typedefof<string []>)
        serializer.WriteObject (ms, arrStr)
        System.Text.Encoding.UTF8.GetString (ms.ToArray ())

    let private stringsToQuotedArray (strings:string list) =
        strings |> List.map (sprintf "\"%s\"") |> String.concat ", " |> sprintf "[%s]"
    
    let private stringContainsWhitepace (s:string) =
        if isNull (s) then
            false
        else
            System.Linq.Enumerable.Any(s, fun c -> System.Char.IsWhiteSpace(c))

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
        | Copy (SingleSource (source), dest, from) ->
            if source |> stringContainsWhitepace || dest |> stringContainsWhitepace then
                [source; dest] |> stringsToQuotedArray
                |> sprintf "COPY %s"
            else
                sprintf "COPY %s %s" source dest
        | Copy (MultipleSources (sources), dest, from) ->
            if sources |> List.exists(stringContainsWhitepace) || dest |> stringContainsWhitepace then
                sources@[dest] |> stringsToQuotedArray
                |> sprintf "COPY %s"
            else
                sources@[dest] |> String.concat " "
                |> sprintf "COPY %s"
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
        | Onbuild(instruction) ->
            instruction |> printInstruction |> sprintf "ONBUILD %s"
        | Stopsignal(_) -> failwith "Not Implemented"
        | Shell(executable, parameters) -> failwith "Not Implemented"
        | Healthcheck(_) -> failwith "Not Implemented"

    /// Concatenates a list of Dockerfile instructions into a single Dockerfile
    let buildDockerfile (instructions:Instruction list) =
        instructions |> List.map (printInstruction) |> String.concat System.Environment.NewLine
