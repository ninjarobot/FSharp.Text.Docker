namespace FSharp.Docker

open System

module Dockerfile =

    type Command =
        | ShellCommand of string
        | Exec of Executable:string * Arg:string list

    type KeyVal =
        | KeyValuePair of Key:string * Value:string
        | Dictionary of Map<string,string>

    type Add =
        | SingleSource of Source:string * Destination:string
        | MultipleSources of Source:string list * Destination:string

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
        | Add of Add
        | Copy of Source:string * Destination:string * From:BuildStage option
        | Entrypoint of Command
        | Volume of string list
        | User of Username:string * Group:string option
        | WorkDir of string
        | Arg of Name:string * Default:string option
        | Onbuild of Instruction
        | Stopsignal of Signal
        | Shell of Executable:string * Parameters:string list
        | Healthcheck of Healthcheck
    
    let private writeInstruction (instruction:Instruction) =
        match instruction with
        | From (img, Some (tag), Some(name)) -> sprintf "FROM %s:%s AS %s" img tag name
        | From (img, None, Some(name)) -> sprintf "FROM %s AS %s" img name
        | From (img, Some(tag), None) -> sprintf "FROM %s:%s" img tag
        | From (img, None, None) -> sprintf "FROM %s" img
        | Run (ShellCommand (command)) -> sprintf "RUN %s" command
        | Run (Exec (executable, args)) -> 
            match args with
            | [] -> sprintf """RUN ["%s"]""" executable
            | _ -> 
                args
                |> List.map (sprintf "\"%s\"") // pad with quotes
                |> String.concat ", " // put commas between quoted args
                |> sprintf """RUN ["%s", %s]""" executable // and append to RUN
        | Cmd (ShellCommand (command)) -> sprintf "CMD %s" command
        | Cmd (Exec (executable, args)) -> 
            match args with
            | [] -> sprintf """CMD ["%s"]""" executable
            | _ -> 
                args
                |> List.map (sprintf "\"%s\"") // pad with quotes
                |> String.concat ", " // put commas between quoted args
                |> sprintf """CMD ["%s", %s]""" executable // and append to CMD
        | CmdArgs(args) ->
            args
            |> List.map (sprintf "\"%s\"") // pad with quotes
            |> String.concat "," // put commas between quoted args
            |> sprintf """CMD [%s]""" // and append to CMD
        | Label(_) -> failwith "Not Implemented"
        | Expose(ports) -> failwith "Not Implemented"
        | Env(_) -> failwith "Not Implemented"
        | Add(_) -> failwith "Not Implemented"
        | Copy(source, destination, from) -> failwith "Not Implemented"
        | Entrypoint(_) -> failwith "Not Implemented"
        | Volume(_) -> failwith "Not Implemented"
        | User(username, group) -> failwith "Not Implemented"
        | WorkDir(_) -> failwith "Not Implemented"
        | Arg(name, ``default``) -> failwith "Not Implemented"
        | Onbuild(_) -> failwith "Not Implemented"
        | Stopsignal(_) -> failwith "Not Implemented"
        | Shell(executable, parameters) -> failwith "Not Implemented"
        | Healthcheck(_) -> failwith "Not Implemented"


    let buildDockerfile (instructions:Instruction list) =
        instructions |> List.map (writeInstruction) |> String.concat Environment.NewLine

module Program =

    open Dockerfile

    [<EntryPoint>]
    let main argv =
        [
            From ("debian", Some("stretch-slim"), None)
            Run (Exec ("apt-get", ["update"]))
            Run (Exec ("apt-get", ["install"; "-y"; "wget"]))
            Cmd (ShellCommand ("wget https://github.com/fsharp/fsharp/archive/4.1.25.tar.gz"))
        ]
        |> Dockerfile.buildDockerfile
        |> printfn "%A"
        0 // return an integer exit code
