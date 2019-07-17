module SlackVoteCommands

open DDDApi
open Voting
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open FSharp.Azure.Storage.Table
open Microsoft.AspNetCore.Mvc

let sessionIdMap = fun (s: SessionV2, _) -> s.SessionizeId
let voteExists sessions (vote: Vote) =
    sessions
    |> Seq.map sessionIdMap
    |> Seq.exists (fun id -> vote.SessionId = id)

type SessionVote =
     { Title: string
       Presenter: string
       TrackLength: string
       SessionId: string
       TicketVote: bool
       Track: string}

type VoteResult =
     { Title: string
       Presenter: string
       TrackLength: string
       TotalVotes: int
       TicketHolderVotes: int
       NonTicketHolderVotes: int
       Track: string}

type SlackMessage =
    { Text: string
      response_type: string }

let countVotes votes sessions presenters =
  votes
  |> Seq.filter(fun (vote, _) -> vote |> voteExists sessions)
  |> Seq.map(fun (vote, _) -> 
            let (session, _) = sessions |> Seq.find(fun (session, _) -> session.SessionizeId = vote.SessionId)
            let presenterNames =
                presenters
                |> Seq.filter (fun (p, _) -> p.TalkId = session.SessionizeId)
                |> Seq.map (fun (p, _) -> (sprintf "%s (%s)" p.FullName p.PreferredPronoun))
                |> String.concat ", "
            { Title = session.Title
              Presenter = presenterNames
              TrackLength = session.SessionLength
              SessionId = session.SessionizeId
              TicketVote = match vote.TicketNumber with
                           | null | "" -> false
                           | _ -> true
              Track = session.Track })
  |> Seq.groupBy(fun r -> r.Title)
  |> Seq.map(fun (key, sessionVote) ->
             let ticketVotes = sessionVote |> Seq.filter(fun s -> s.TicketVote) |> Seq.length
             let nonTicketVotes = sessionVote |> Seq.filter(fun s -> not s.TicketVote) |> Seq.length
             let voteInfo = sessionVote |> Seq.head // the rest of the info we can get from just the first session
             { Title = key
               Presenter = voteInfo.Presenter
               TrackLength = voteInfo.TrackLength
               TotalVotes = (ticketVotes * 2) + nonTicketVotes
               TicketHolderVotes = ticketVotes
               NonTicketHolderVotes = nonTicketVotes
               Track = voteInfo.Track })
   |> Seq.sortByDescending(fun r -> r.TotalVotes)

let formatVotes countedVotes =
    match Seq.length countedVotes with
    | 0 -> "No Votes yet :("
    | _ ->
        let titleLength =
            countedVotes
            |> Seq.map(fun v -> v.Title.Length)
            |> Seq.sortByDescending id
            |> Seq.head
        let presenterLength =
            countedVotes
            |> Seq.map(fun v -> v.Presenter.Length)
            |> Seq.sortByDescending id
            |> Seq.head
        countedVotes
        |> Seq.map (fun v -> sprintf "%5d | %3d | %4d | %-*s | %-*s | %s" v.TotalVotes v.TicketHolderVotes v.NonTicketHolderVotes titleLength v.Title presenterLength v.Presenter v.TrackLength)
        |> String.concat "\r\n"
        |> sprintf "```\r\nTotal | THV | NTHV | %-*s | %-*s | Session Length\r\n%s\r\n```" titleLength "Title" presenterLength "Presenter"

[<FunctionName("Slack_Get_Votes_Summary")>]
let getVotesSummary
    ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = "v2/Slack-Votes")>] req: HttpRequest)
    ([<Table("Session", Connection = "EventStorage")>] sessionsTable)
    ([<Table("Presenter", Connection = "EventStorage")>] presentersTable)
    ([<Table("Vote", Connection = "EventStorage")>]votesTable) =

    let year = req.Form.["text"].[0]

    let votes =
        Query.all<Vote>
        |> Query.where<@ fun v _ -> v.Year = year @>
        |> fromTableToClient votesTable

    let sessions =
        Query.all<SessionV2>
        |> Query.where <@ fun s _ -> s.EventYear = year && s.Status = "Approved" @>
        |> fromTableToClient sessionsTable

    let presenters = 
        Query.all<Presenter>
        |> Query.where<@ fun p _ -> p.EventYear = year @>
        |> fromTableToClient presentersTable

    let voteBreakdown = countVotes votes sessions presenters

    let devVotes =
        voteBreakdown
        |> Seq.filter (fun vote -> vote.Track = "Development")
        |> Seq.truncate 10

    let jdVotes =
        voteBreakdown
        |> Seq.filter (fun vote -> vote.Track = "Junior Dev")
        |> Seq.truncate 5

    let dataDesignVotes =
        voteBreakdown
        |> Seq.filter (fun vote -> vote.Track = "Data" || vote.Track = "Design")
        |> Seq.truncate 5

    let txt =
        sprintf
            ":mega: Dev\r\n%s\r\n:mega: Junior Dev\r\n%s\r\n:mega: Data & Design\r\n%s"
            (formatVotes devVotes)
            (formatVotes jdVotes)
            (formatVotes dataDesignVotes)

    OkObjectResult(
        { Text = txt
          response_type = "in_channel" }
    ) :> IActionResult
