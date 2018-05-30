#r "System.Net.Http"
#r "Microsoft.WindowsAzure.Storage"
#load "../SharedCode/SessionTableEntity.fsx"
#load "../SharedCode/SessionResponse.fsx"

open SessionTableEntity
open SessionResponse

open System.Linq
open System.Net
open System.Net.Http
open Microsoft.WindowsAzure.Storage.Table

let Run(req: HttpRequestMessage, inTable: IQueryable<Session>, log: TraceWriter) =
    let q =
            req.GetQueryNameValuePairs()
                |> Seq.tryFind (fun kv -> kv.Key = "year")
    match q with
    | Some kv ->  
        let pk = sprintf "Session-%s" kv.Value
        let sessions =
            query {
                for session in inTable do
                where (session.PartitionKey = pk)
                where (session.Status = 1)
                select session
            }
            |> Seq.map(fun session -> { Id = session.RowKey;
                                        SessionTitle = session.SessionTitle;
                                        SessionAbstract = session.SessionAbstract;
                                        PresenterName = session.PresenterName;
                                        PresenterBio = session.PresenterBio;
                                        RecommendedAudience = session.RecommendedAudience;
                                        PresenterTwitterAlias = session.PresenterTwitterAlias;
                                        PresenterWebsite = session.PresenterWebsite
                                        Year = session.PartitionKey.Replace("Session-", "");
                                        SessionLength = session.SessionLength;
                                        TrackType = session.TrackType })

        match Seq.length sessions with
        | 0 -> req.CreateErrorResponse(HttpStatusCode.NotFound, "We didn't have an event that year")
        | _ -> req.CreateResponse(HttpStatusCode.OK, sessions)
    | None -> req.CreateErrorResponse(HttpStatusCode.BadRequest, "No year provided")
