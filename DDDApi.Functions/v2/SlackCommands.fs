module SlackCommands

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open FSharp.Azure.Storage.Table
open DDDApi
open System.Runtime.Serialization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

type SlackAccessoryTextBlock =
     { Type: string
       Text: string
       Emoji: bool }

type SlackAccessoryBlock =
     { Type: string
       Text: SlackAccessoryTextBlock
       Value: string }

type SlackTextBlock =
     { Type: string
       Text: string }

[<DataContract>]
type SlackBlockWithAccessory =
     { [<field: DataMember(Name="type")>]BlockType: string
       [<field: DataMember(Name="text")>]TextBlock: SlackTextBlock
       [<field: DataMember(Name="accessory")>]AccessoryBlock: SlackAccessoryBlock }

[<DataContract>]
type SlackBlockTextOnly =
     { [<field: DataMember(Name="type")>]BlockType: string
       [<field: DataMember(Name="text")>]TextBlock: SlackTextBlock }

let sessionToApprovalMessage s presenters =
    let presenterNames = presenters
                         |> Seq.map (fun p -> p.FullName)
                         |> String.concat ", "
    { BlockType = "section"
      TextBlock =
       { Type = "mrkdwn"
         Text = sprintf "(%s) _%s_ by *%s*" s.SessionizeId s.Title presenterNames }
      AccessoryBlock =
       { Type = "button"
         Text = { Type = "plain_text"; Text = ":heavy_check_mark: Approve"; Emoji = true }
         Value = s.SessionizeId } }

let sessionToViewMessage session presenters =
    presenters
    |> Seq.map (fun p -> p.FullName)
    |> String.concat ", "
    |> sprintf "(%s) _%s_ by *%s*" session.SessionizeId session.Title

let sessionToDetailMessage s presenters =
    let presenterNames = presenters
                         |> Seq.map (fun p -> p.FullName)
                         |> String.concat ", "
    { BlockType = "section"
      TextBlock =
       { Type = "mrkdwn"
         Text = sprintf "_%s_ by *%s*\r\n%s\r\n> Topic: %s\r\n> Track: %s\r\n> Length: %s" s.Title presenterNames s.Abstract s.Topic s.Track s.SessionLength } }

type SlackMessageWithAccessory =
    { Text: string
      Blocks: seq<SlackBlockWithAccessory> }

[<FunctionName("Slack_Unapproved_Sessions")>]
let unapprovedSessionsCommand ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = "v2/Slack-UnapprovedSession")>] req: HttpRequest)
                              ([<Table("Session", Connection = "EventStorage")>] sessionsTable)
                              ([<Table("Presenter", Connection = "EventStorage")>] presentersTable) =
     async {
         let year = req.Form.["text"].[0]

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
                                 |> sessionToApprovalMessage s)

         match Seq.length resultSessions with
         | 0 -> return OkObjectResult(":boom: There are no unapproved sessions") :> IActionResult
         | _ -> return OkObjectResult({ Text = "Here are the session"
                                        Blocks = match Seq.length resultSessions with
                                                 | len when len > 50 -> resultSessions |> Seq.take 50
                                                 | _ -> resultSessions }) :> IActionResult
     } |> Async.StartAsTask

type SlackMessageTextOnly =
    { Text: string
      Blocks: seq<SlackBlockTextOnly> }

[<FunctionName("Slack_Approved_Sessions")>]
let approvedSessionsCommand ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = "v2/Slack-ApprovedSession")>] req: HttpRequest)
                            ([<Table("Session", Connection = "EventStorage")>] sessionsTable)
                            ([<Table("Presenter", Connection = "EventStorage")>] presentersTable) =
     async {
         let year = req.Form.["text"].[0]

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
                                  |> sessionToViewMessage s)

         match Seq.length resultSessions with
         | 0 -> return OkObjectResult(":boom: There are no approved sessions") :> IActionResult
         | _ -> return OkObjectResult(resultSessions |> String.concat "\r\n") :> IActionResult
     } |> Async.StartAsTask

[<FunctionName("Slack_Approve_Session")>]
let approveSessionCommand ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = "v2/Slack-ApproveSession")>] req: HttpRequest)
                          ([<Table("Session", Connection = "EventStorage")>] sessionsTable) =
    async {
        let id = req.Form.["text"].[0]

        let! sessions = Query.all<SessionV2>
                        |> match id with
                           | "all" -> Query.take 1000
                           | _ -> Query.where <@ fun s _ -> s.SessionizeId = id @>
                        |> fromTableToClientAsync sessionsTable

        let approvedSessions = sessions
                               |> Seq.map(fun (s, _) -> { s with Status = "Approved" })

        match Seq.length approvedSessions with
        | 0 -> return OkObjectResult(":boom: There are no unapproved sessions") :> IActionResult
        | _ ->
            let! _ = approvedSessions
                     |> Seq.map InsertOrMerge
                     |> autobatch
                     |> List.map (inTableToClientAsBatchAsync sessionsTable)
                     |> Async.Parallel
            return OkObjectResult(":tada: Sessions approved") :> IActionResult
    } |> Async.StartAsTask

[<FunctionName("Slack_Get_Session")>]
let getSessionCommand([<HttpTrigger(AuthorizationLevel.Function, "post", Route = "v2/Slack-Session")>] req: HttpRequest,
                      [<Table("Session", Connection = "EventStorage")>] sessionsTable,
                      [<Table("Presenter", Connection = "EventStorage")>] presentersTable) =
    async {
        let id = req.Form.["text"].[0]
        let! sessions = Query.all<SessionV2>
                        |> Query.where <@ fun s _ -> s.SessionizeId = id @>
                        |> fromTableToClientAsync sessionsTable

        let! presenters = Query.all<Presenter>
                          |> Query.where<@ fun p _ -> p.TalkId = id @>
                          |> fromTableToClientAsync presentersTable

        let resultSessions = sessions
                             |> Seq.map(fun (s, _) ->
                                presenters
                                |> Seq.filter (fun (p, _) -> p.TalkId = s.SessionizeId)
                                |> Seq.map (fun (p, _) -> p)
                                |> sessionToDetailMessage s)

        match Seq.length resultSessions with
        | 0 -> return NotFoundObjectResult("No session found") :> IActionResult
        | _ -> return OkObjectResult({ Text = "Here are the sessions"
                                       Blocks = resultSessions }) :> IActionResult
    } |> Async.StartAsTask
