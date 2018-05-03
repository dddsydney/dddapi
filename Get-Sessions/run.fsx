#r "System.Net.Http"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Runtime.Serialization"

open System.Linq
open System.Net
open System.Net.Http
open Microsoft.WindowsAzure.Storage.Table
open System.Runtime.Serialization

type Session() =
    inherit TableEntity()
    member val SessionTitle: string = null with get, set
    member val SessionAbstract: string = null with get, set
    member val RecommendedAudience: string = null with get, set
    member val PresenterName: string = null with get, set
    member val PresenterEmail: string = null with get, set
    // member val PresenterMobileNumber: string = null with get, set
    member val PresenterTwitterAlias: string = null with get, set
    member val PresenterWebsite: string = null with get, set
    member val PresenterBio: string = null with get, set
    member val SubmittedDateUtc: DateTime = DateTime.MinValue with get, set
    member val Status: int = 0 with get, set
    // member val SubmitterIp: string = null with get, set

[<DataContract>]
type ResponseSessions =
    { [<field: DataMember(Name="SessionId")>]Id: string
      [<field: DataMember(Name="SessionTitle")>]SessionTitle: string
      [<field: DataMember(Name="SessionAbstract")>]SessionAbstract: string
      [<field: DataMember(Name="PresenterName")>]PresenterName: string
      [<field: DataMember(Name="PresenterBio")>]PresenterBio: string
      [<field: DataMember(Name="PresenterTwitterAlias")>]PresenterTwitterAlias: string
      [<field: DataMember(Name="PresenterWebsite")>]PresenterWebsite: string
      [<field: DataMember(Name="RecommendedAudience")>]RecommendedAudience: string }

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
                                        PresenterWebsite = session.PresenterWebsite })

        match Seq.length sessions with
        | 0 -> req.CreateErrorResponse(HttpStatusCode.NotFound, "We didn't have an event that year")
        | _ -> req.CreateResponse(HttpStatusCode.OK, sessions)
    | None -> req.CreateErrorResponse(HttpStatusCode.BadRequest, "No year provided")
