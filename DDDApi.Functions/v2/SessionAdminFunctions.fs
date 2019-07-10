module SessionAdminFunctions

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open FSharp.Azure.Storage.Table
open DDDApi
open DDDApi.azureTableUtils
open DDDApi.ResponseSessionMapper
open Microsoft.AspNetCore.Mvc

[<FunctionName("Get_unapproved_sessions")>]
let getUnapprovedSessions ([<HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "v2/Get-UnapprovedSession/{year}")>] req: HttpRequest)
                          ([<Table("Session", Connection = "EventStorage")>] sessionsTable)
                          ([<Table("Presenter", Connection = "EventStorage")>] presentersTable)
                          (year: string) =
     async {
         let! sessions = Query.all<SessionV2>
                         |> Query.where <@ fun s _ -> s.EventYear = year && s.Status = "Unapproved" @>
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
         | 0 -> return NotFoundObjectResult("There are no unapproved sessions") :> IActionResult
         | _ -> return OkObjectResult(resultSessions) :> IActionResult
     } |> Async.StartAsTask

[<FunctionName("Approve_Session")>]
let approveSession ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = "v2/Set-ApprovedSession/{id}")>] req: HttpRequest)
                   ([<Table("Session", Connection = "EventStorage")>] sessionsTable)
                   (id: string) =

    async {
        let! sessions = Query.all<SessionV2>
                        |> Query.where <@ fun s _ -> s.SessionizeId = id @>
                        |> fromTableToClientAsync sessionsTable

        let approvedSessions = sessions
                               |> Seq.map(fun (s, _) -> { s with Status = "Approved" })


        match Seq.length approvedSessions with
        | 0 -> return NotFoundObjectResult("There are no unapproved sessions") :> IActionResult
        | _ ->
            let! _ = approvedSessions
                     |> Seq.map InsertOrMerge
                     |> autobatch
                     |> List.map (inTableToClientAsBatchAsync sessionsTable)
                     |> Async.Parallel
            return OkObjectResult(approvedSessions) :> IActionResult
    } |> Async.StartAsTask