module LetsEncrypt

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open System.IO
open Microsoft.AspNetCore.Mvc

[<FunctionName("Lets_Encrypt")>]
let letsEncrypt
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "letsencrypt/{code}")>] req: HttpRequest)
    (context: ExecutionContext)
    (code: string) =

    let path = Path.Combine(context.FunctionDirectory, "..", "letsencrypt", ".well-known", "acme-challenge", code)
    
    match File.Exists path with
    | true ->
        let res = path
                    |> File.ReadAllText
                    |> JsonResult
        res :> IActionResult
    | false -> NotFoundResult() :> IActionResult