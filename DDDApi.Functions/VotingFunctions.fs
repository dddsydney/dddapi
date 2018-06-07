namespace DDDApi.Functions
open DDDApi
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host

open System
// open System.ServiceModel.Channels 
open System.Linq
open System.Net
open System.Net.Http
open Microsoft.WindowsAzure.Storage.Table
open Newtonsoft.Json

type UserVote = { TicketNumber: string
                  SessionIds: array<string> }

module VotingFunctions =

    let Run(req: HttpRequestMessage, sessionsTable: IQueryable<Session>, votesTable: ICollector<Vote>, log: TraceWriter) =
        async {
            let q =
                req.GetQueryNameValuePairs()
                    |> Seq.tryFind (fun kv -> kv.Key = "year")
            match q with
            | Some kv ->
                //let now = DateTimeOffset.Now
                let now = DateTimeOffset(2018, 06, 14, 08, 00, 00, 00, TimeSpan.FromHours(8.0))

                let year = kv.Value
                
                log.Info(sprintf "Looking for votes in %s" year)

                match validVotingPeriod now year with
                | true ->
                    let! content = req.Content.ReadAsStringAsync() |> Async.AwaitTask
                    let userVote = content |> JsonConvert.DeserializeObject<UserVote>
                    let ids = userVote.SessionIds
                    
                    log.Info(userVote.TicketNumber)

                    //let ip = req.Properties.[RemoteEndpointMessageProperty.Name] :?> RemoteEndpointMessageProperty

                    //log.Info(ip.Address)

                    let sessionsPartionKey = sprintf "Session-%s" year

                    log.Info(sessionsPartionKey)

                    let sessions =
                            query {
                                for session in sessionsTable do
                                where (session.PartitionKey = sessionsPartionKey)
                                where (session.Status = 1)
                                select session
                            }

                    let votedSessions = sessions |> Seq.filter(fun s -> ids |> Array.exists(fun id -> id = s.RowKey))

                    match Seq.length votedSessions = Array.length ids with
                    | true ->
                        ids |> Seq.iter (fun id ->
                                                let vote = { PartitionKey = year
                                                             RowKey = Guid.NewGuid().ToString()
                                                             SessionId = Guid(id)
                                                             IpAddress = "" //ip.Address
                                                             SubmittedDateUTC = DateTimeOffset.Now
                                                             TicketNumber = userVote.TicketNumber }
                                                votesTable.Add vote)

                        return req.CreateResponse(HttpStatusCode.Created)
                    | false -> return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Voted sessions are outside of the current year")
                | false -> return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Voting for that year is closed")
            | None -> return req.CreateErrorResponse(HttpStatusCode.BadRequest, "No year provided")

        } |> Async.StartAsTask
