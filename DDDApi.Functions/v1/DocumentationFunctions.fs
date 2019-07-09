namespace DDDApi

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open System.IO

module DocumentationFunctions =

    [<FunctionName("Get_Swagger")>]
    let getSwagger([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/Get-Swagger")>] req: HttpRequest,
                   context: ExecutionContext) =

        let path = Path.Combine(context.FunctionDirectory, ".azurefunctions", "swagger", "swagger-v1.json")

        match File.Exists path with
        | true ->
            let res = path
                      |> File.ReadAllText
                       |> JsonResult
            res :> IActionResult
        | false -> NotFoundResult() :> IActionResult