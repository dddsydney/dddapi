namespace DDDApi.Functions
open DDDApi

open System.Net.Http
open Microsoft.WindowsAzure.Storage.Table
open Newtonsoft.Json
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.Azure.WebJobs.Host
open System
open System.IO
open DDDApi.Voting
open FSharp.Azure.Storage.Table
open DDDApi.azureTableUtils

type UserVote = { TicketNumber: string
                  SessionIds: array<string> }

module VotingFunctions =
    let getIpAddress (req: HttpRequest) =
        let ip = req.HttpContext.Connection.RemoteIpAddress.MapToIPv4()
        ip.ToString()

    [<FunctionName("Vote_for_session")>]
    let saveVote([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Save-Vote/{year}")>] req: HttpRequest,
                 [<Table("Sessions")>]sessionsSource: CloudTable,
                 [<Table("Votes")>]votesTable: CloudTable,
                 year: string,
                 log: TraceWriter) =
        async {
            let now = DateTimeOffset.Now
            log.Info(sprintf "Looking for votes in %s" year)

            match validVotingPeriod now year with
            | true ->
                use reader = new StreamReader(req.Body)
                let! content = reader.ReadToEndAsync() |> Async.AwaitTask

                let userVote = content |> JsonConvert.DeserializeObject<UserVote>
                let ids = userVote.SessionIds

                let sessionsPartionKey = sprintf "Session-%s" year

                let! sessions = Query.all<Session>
                               |> Query.where <@ fun s m -> m.PartitionKey = sessionsPartionKey && s.Status = 1 @>
                               |> fromTableToClientAsync sessionsSource

                let votedSessions = sessions |> Seq.filter(fun (s, _) -> ids |> Array.exists(fun id -> id = s.RowKey))

                match Seq.length votedSessions = Array.length ids with
                | true ->
                    let ipAddress = getIpAddress req
                    ids |> Seq.iter (fun id -> let vote = { PartitionKey = year
                                                            RowKey = Guid.NewGuid().ToString()
                                                            SessionId = Guid(id)
                                                            IpAddress = ipAddress
                                                            SubmittedDateUTC = DateTimeOffset.Now
                                                            TicketNumber = userVote.TicketNumber }
                                               vote
                                               |> Insert
                                               |> inTableToClientAsync votesTable
                                               |> Async.RunSynchronously
                                               |> ignore)

                    return StatusCodeResult(201) :> IActionResult
                | false -> return BadRequestObjectResult("Voted sessions are outside of the current year") :> IActionResult
            | false -> return BadRequestObjectResult("Voting for that year is closed") :> IActionResult
        } |> Async.StartAsTask
