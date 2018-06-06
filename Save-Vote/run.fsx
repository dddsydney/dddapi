#load "./Vote.fsx"
#load "./VotingPeriods.fsx"
#load "../SharedCode/SessionTableEntity.fsx"

#r "System.Net.Http"
#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"
#r "System.ServiceModel"

open SessionTableEntity
open Vote
open VotingPeriods

open System
open System.ServiceModel.Channels 
open System.Linq
open System.Net
open System.Net.Http
open Microsoft.WindowsAzure.Storage.Table
open Newtonsoft.Json

type UserVote = { TicketNumber: string
                  SessionIds: array<string> }

let Run(req: HttpRequestMessage, sessionsTable: IQueryable<Session>, votesTable: ICollector<Vote>, log: TraceWriter) =
    async {
        let q =
            req.GetQueryNameValuePairs()
                |> Seq.tryFind (fun kv -> kv.Key = "year")
        match q with
        | Some kv ->
            let now = DateTimeOffset.Now
            //let now = DateTimeOffset(2018, 06, 14, 08, 00, 00, 00, TimeSpan.FromHours(8.0))

            let year = kv.Value

            match validVotingPeriod now year with
            | true ->
                let! content = req.Content.ReadAsStringAsync() |> Async.AwaitTask
                let userVote = content |> JsonConvert.DeserializeObject<UserVote>
                let ids = userVote.SessionIds

                let sessionsPartionKey = sprintf "Session-%s" year
                let sessions =
                        query {
                            for session in sessionsTable do
                            where (session.PartitionKey = sessionsPartionKey)
                            where (session.Status = 1)
                            select session
                        }

                let ip = req.Properties.[RemoteEndpointMessageProperty.Name] :?> RemoteEndpointMessageProperty

                let votedSessions = sessions |> Seq.filter(fun s -> ids |> Array.exists(fun id -> id = s.RowKey))

                match Seq.length votedSessions = Array.length ids with
                | true ->
                    ids |> Seq.iter (fun id ->
                                            let vote = { PartitionKey = year
                                                         RowKey = Guid.NewGuid().ToString()
                                                         SessionId = Guid(id)
                                                         IpAddress = ip.Address
                                                         SubmittedDateUTC = DateTimeOffset.Now
                                                         TicketNumber = userVote.TicketNumber }
                                            votesTable.Add vote)

                    return req.CreateResponse(HttpStatusCode.Created)
                | false -> return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Voted sessions are outside of the current year")
            | false -> return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Voting for that year is closed")
        | None -> return req.CreateErrorResponse(HttpStatusCode.BadRequest, "No year provided")

    } |> Async.StartAsTask
