namespace DDDApi.Functions

open DDDApi

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.WindowsAzure.Storage.Table
open DDDApi.ResponseSessionMapper

module SessionFunctions =
    [<FunctionName("Get_sessions_for_a_year")>]
    let getSessions([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Get-Sessions/{year}")>] req: HttpRequest,
                    [<Table("Sessions")>]sessionsTable: CloudTable,
                    year: string) =
        let pk = sprintf "Session-%s" year

        let query = (new TableQuery<Session>())
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk))

        async {
            let! sessions = sessionsTable.ExecuteQuerySegmentedAsync(query, null) |> Async.AwaitTask

            let resultSessions = sessions
                                |> Seq.map sessionToResult

            match Seq.length resultSessions with
            | 0 -> return NotFoundObjectResult("We didn't have an event that year") :> IActionResult
            | _ -> return OkObjectResult(resultSessions) :> IActionResult
        } |> Async.StartAsTask

    [<FunctionName("Get_session_by_id")>]
    let getSession([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Get-Session/{id}")>] req: HttpRequest,
                    [<Table("Sessions")>]sessionsTable: CloudTable,
                    id: string) =
        let query = (new TableQuery<Session>())
                        .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id))

        async {
            let! sessions = sessionsTable.ExecuteQuerySegmentedAsync(query, null) |> Async.AwaitTask

            let resultSessions = sessions
                                |> Seq.map sessionToResult

            match Seq.length resultSessions with
            | 0 -> return NotFoundObjectResult("No session found") :> IActionResult
            | _ -> return OkObjectResult(resultSessions |> Seq.item 0) :> IActionResult
        } |> Async.StartAsTask
