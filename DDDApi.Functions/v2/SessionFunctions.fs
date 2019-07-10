module SessionFunctions

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.WindowsAzure.Storage.Table
open FSharp.Azure.Storage.Table
open DDDApi
open DDDApi.azureTableUtils
open DDDApi.ResponseSessionMapper
open Microsoft.AspNetCore.Mvc

[<FunctionName("Get_sessions_for_a_year_v2")>]
let getSessionsV2([<HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "v2/Get-Sessions/{year}")>] req: HttpRequest,
                  [<Table("Session", Connection = "EventStorage")>] sessionsTable: CloudTable,
                  [<Table("Presenter", Connection = "EventStorage")>] presentersTable: CloudTable,
                  year: string) =
    async {
        let! sessions = Query.all<SessionV2>
                        |> Query.where <@ fun s _ -> s.EventYear = year && s.Status = "Approved" @>
                        |> fromTableToClientAsync sessionsTable

        let! presenters = Query.all<Presenter>
                            |> Query.where<@ fun p _ -> p.EventYear = year @>
                            |> fromTableToClientAsync presentersTable

        let resultSessions = sessions
                            |> Seq.map(fun (s, _) ->
                                presenters
                                            |> Seq.filter (fun (p, _) -> p.TalkId = s.SessionizeId)
                                            |> Seq.map (fun (p, _) -> p)
                                            |> sessionV2ToResult s)

        match Seq.length resultSessions with
        | 0 -> return NotFoundObjectResult("We didn't have an event that year") :> IActionResult
        | _ -> return OkObjectResult(resultSessions) :> IActionResult
    } |> Async.StartAsTask

[<FunctionName("Get_session_by_id_v2")>]
let getSessionV2([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/Get-Session/{id}")>] req: HttpRequest,
                  [<Table("Session", Connection = "EventStorage")>] sessionsTable: CloudTable,
                  [<Table("Presenter", Connection = "EventStorage")>] presentersTable: CloudTable,
                  id: string) =
    async {
        let! sessions = Query.all<SessionV2>
                        |> Query.where <@ fun s _ -> s.Status = "Approved" && s.SessionizeId = id @>
                        |> fromTableToClientAsync sessionsTable

        let! presenters = Query.all<Presenter>
                            |> Query.where<@ fun p _ -> p.TalkId = id @>
                            |> fromTableToClientAsync presentersTable

        let resultSessions = sessions
                            |> Seq.map(fun (s, _) ->
                                presenters
                                            |> Seq.filter (fun (p, _) -> p.TalkId = s.SessionizeId)
                                            |> Seq.map (fun (p, _) -> p)
                                            |> sessionV2ToResult s)

        match Seq.length resultSessions with
        | 0 -> return NotFoundObjectResult("No session found") :> IActionResult
        | _ -> return OkObjectResult(resultSessions) :> IActionResult
    } |> Async.StartAsTask