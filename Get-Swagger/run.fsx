#r "System.Net.Http"
#r "Newtonsoft.Json"

open System.Net
open System.Net.Http
open System.IO
open System
open Newtonsoft.Json

let getEnv variable =
    Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process)

let Run(req: HttpRequestMessage) =
    let dir = getEnv "HOME"
    let json = Path.Combine(dir, "site/wwwroot/.azurefunctions/swagger/swagger.json")
               |> File.ReadAllText
               |> JsonConvert.DeserializeObject
    req.CreateResponse(HttpStatusCode.OK, json)
