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
                |> Seq.tryFind (fun kv -> kv.Key = "id")
    match q with
    | Some kv ->
        let id = kv.Value
        let sessions =
            query {
                for session in inTable do
                where (session.RowKey = id)
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
                                        Year = session.PartitionKey.Replace("Session-", "") })

        match Seq.length sessions with
        | 0 -> req.CreateErrorResponse(HttpStatusCode.NotFound, "No matching session")
        | _ -> req.CreateResponse(HttpStatusCode.OK, sessions |> Seq.item 0)
    | None -> req.CreateErrorResponse(HttpStatusCode.BadRequest, "No session ID was provided")
