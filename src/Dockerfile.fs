namespace FSharp.Docker

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

    let private stringsToJsonArray (strings:string list) =
        use ms = new System.IO.MemoryStream ()
        let arrStr = strings |> Array.ofList
        let serializer = System.Runtime.Serialization.Json.DataContractJsonSerializer (typedefof<string []>)
        serializer.WriteObject (ms, arrStr)
        System.Text.Encoding.UTF8.GetString (ms.ToArray ())
    
    let printInstruction (instruction:Instruction) =
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
        | CmdArgs(args) ->
            sprintf "CMD %s" (args |> stringsToJsonArray)
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

    /// Concatenates a list of Dockerfile instructions into a single Dockerfile
    let buildDockerfile (instructions:Instruction list) =
        instructions |> List.map (printInstruction) |> String.concat System.Environment.NewLine
