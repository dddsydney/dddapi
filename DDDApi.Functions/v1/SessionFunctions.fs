namespace DDDApi.Functions

open DDDApi
open DDDApi.ResponseSessionMapper
open DDDApi.azureTableUtils

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.WindowsAzure.Storage.Table
open FSharp.Azure.Storage.Table

module SessionFunctions =
    [<FunctionName("Get_sessions_for_a_year")>]
    let getSessions([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/v1/Get-Sessions/{year}")>] req: HttpRequest,
                    [<Table("LegacySessions", Connection = "EventStorage")>]sessionsTable: CloudTable,
                    year: string) =
        let pk = sprintf "Session-%s" year

        async {
            let! sessions = Query.all<Session>
                            |> Query.where <@ fun g s -> s.PartitionKey = pk @>
                            |> fromTableToClientAsync sessionsTable

            let resultSessions = sessions
                                |> Seq.map(fun (s, _) -> sessionToResult s)

            match Seq.length resultSessions with
            | 0 -> return NotFoundObjectResult("We didn't have an event that year") :> IActionResult
            | _ -> return OkObjectResult(resultSessions) :> IActionResult
        } |> Async.StartAsTask

    [<FunctionName("Get_session_by_id")>]
    let getSession([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/v1/Get-Session/{id}")>] req: HttpRequest,
                    [<Table("Sessions")>]sessionsTable: CloudTable,
                    id: string) =
        async {
            let! sessions = Query.all<Session>
                            |> Query.where <@ fun g s -> s.RowKey = id @>
                            |> fromTableToClientAsync sessionsTable

            let resultSessions = sessions
                                |> Seq.map(fun (s, _) -> sessionToResult s)

            match Seq.length resultSessions with
            | 0 -> return NotFoundObjectResult("No session found") :> IActionResult
            | _ -> return OkObjectResult(resultSessions |> Seq.item 0) :> IActionResult
        } |> Async.StartAsTask
