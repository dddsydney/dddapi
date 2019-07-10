module VotingFunctions

open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.Extensions.Logging
open System
open Voting
open System.IO
open Newtonsoft.Json
open FSharp.Azure.Storage.Table
open DDDApi
open DDDApi.azureTableUtils
open Microsoft.AspNetCore.Mvc

type UserVote =
    { TicketNumber: string
      SessionIds: array<string> }

let getIpAddress (req: HttpRequest) =
    let ip = req.HttpContext.Connection.RemoteIpAddress.MapToIPv4()
    ip.ToString()

[<FunctionName("Vote_for_session")>]
let saveVote([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/Save-Vote/{year}")>] req: HttpRequest,
             [<Table("Session", Connection = "EventStorage")>]sessionsSource,
             [<Table("Vote", Connection = "EventStorage")>]votesTable,
             year: string,
             log: ILogger) =
    async {
        let now = DateTimeOffset.Now
        log.LogInformation(sprintf "Looking for votes in %s" year)

        match validVotingPeriod now year with
        | true ->
            use reader = new StreamReader(req.Body)
            let! content = reader.ReadToEndAsync() |> Async.AwaitTask

            let userVote = content |> JsonConvert.DeserializeObject<UserVote>
            let ids = userVote.SessionIds

            let! sessions = Query.all<SessionV2>
                           |> Query.where <@ fun s _ -> s.EventYear = year && s.Status = "Approved" @>
                           |> fromTableToClientAsync sessionsSource

            let votedSessions = sessions |> Seq.filter(fun (s, _) -> ids |> Array.exists(fun id -> id = s.SessionizeId))

            match Seq.length votedSessions = Array.length ids with
            | true ->
                let ipAddress = getIpAddress req
                let votes = ids |> Seq.map (fun id -> 
                                    let vote =
                                        { Year = year
                                          VoteId = Guid.NewGuid().ToString()
                                          SessionId = Guid(id)
                                          IpAddress = ipAddress
                                          SubmittedDateUTC = DateTimeOffset.Now
                                          TicketNumber = userVote.TicketNumber }
                                    vote |> Insert)
                let! _ = votes
                         |> autobatch
                         |> List.map (inTableToClientAsBatchAsync votesTable)
                         |> Async.Parallel

                return StatusCodeResult(201) :> IActionResult
            | false -> return BadRequestObjectResult("Voted sessions are outside of the current year") :> IActionResult
        | false -> return BadRequestObjectResult("Voting for that year is closed") :> IActionResult
    } |> Async.StartAsTask
