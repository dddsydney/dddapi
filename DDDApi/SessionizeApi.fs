namespace DDDApi

open FSharp.Data
open System
open Microsoft.Extensions.Logging
open azureTableUtils
open FSharp.Azure.Storage.Table

module SessionizeApi =
    type Sessionize = JsonProvider<"../sessionize-sample.json">
    
    let downloadSessionize apiKey =
        Sessionize.AsyncLoad (sprintf "https://sessionize.com/v2/%s/view/all" apiKey)

    let findSpeakers (session : SessionV2) (allSpeakers: array<Sessionize.Speaker>) =
        allSpeakers
        |> Array.filter (fun speaker -> speaker.Sessions
                                        |> Array.exists (fun sessionId -> session.SessionizeId = sessionId.ToString()))

    let getSpeakerLink title (speaker: Sessionize.Speaker) =
        speaker.Links |> Array.findOrNone (fun link -> link.Title = title)

    let getCategoryItem (categories: array<Sessionize.Category>) items title=
        match categories |> Array.findOrNone (fun c -> c.Title = title) with
        | Some category -> category.Items |> Array.findOrNone (fun ci -> items |> Array.exists(fun i -> i = ci.Id))
        | None -> None

    let getQuestionAnswer  (questions: array<Sessionize.Question>) (items: array<Sessionize.QuestionAnswer>) question =
        match questions |> Array.findOrNone (fun q -> q.Question = question) with
        | Some q -> items |> Array.findOrNone (fun i -> i.QuestionId = q.Id)
        | None -> None

    let makeSpeakers allSpeakers categories questions session =
        let remoteSpeakers = findSpeakers session allSpeakers
        
        let gci = getCategoryItem categories
        let gqa = getQuestionAnswer questions

        let speakers = remoteSpeakers
                       |> Array.map (fun speaker ->
                          { EventYear = "2019"
                            TalkId = session.SessionizeId
                            Id = speaker.Id.ToString()
                            FirstName = speaker.FirstName
                            LastName = speaker.LastName
                            FullName = speaker.FullName
                            Bio = speaker.Bio
                            Email = ""
                            Url = match getSpeakerLink "Blog" speaker with
                                  | Some item -> item.Url
                                  | None -> ""
                            Twitter = match getSpeakerLink "Twitter" speaker with
                                      | Some item -> item.Url
                                      | None -> ""
                            LinkedIn = match getSpeakerLink "LinkedIn" speaker with
                                       | Some item -> item.Url
                                       | None -> ""
                            Tagline = speaker.TagLine
                            Photo = speaker.ProfilePicture
                            MobileNumber = match gqa speaker.QuestionAnswers "Mobile Number" with
                                           | Some a -> a.AnswerValue
                                           | None -> ""
                            PreferredPronoun = match gci speaker.CategoryItems "Preferred pronoun" with
                                               | Some item -> item.Name
                                               | None -> ""
                            PreferredPronounOther = ""
                            Level = match gci speaker.CategoryItems "How would you identify your job role" with
                                    | Some item -> item.Name
                                    | None -> ""
                            Experience = match gci speaker.CategoryItems "How much speaking experience do you have?" with
                                         | Some item -> item.Name
                                         | None -> ""
                            UnderRep = match gqa speaker.QuestionAnswers "Are you a member of any underrepresented groups?" with
                                       | Some a -> a.AnswerValue
                                       | None -> "" })

        speakers

    let makeSession categories questions (remoteSession: Sessionize.Session) =
        let gci = getCategoryItem categories
        let gqa = getQuestionAnswer questions

        let session =
            { EventYear = "2019"
              SessionizeId = remoteSession.Id.ToString()
              Title = remoteSession.Title
              Abstract = remoteSession.Description
              RecommendedAudience = match gci remoteSession.CategoryItems "Level" with
                                    | Some item -> item.Name
                                    | None -> ""
              SubmittedDateUtc = DateTime.UtcNow
              Status = "Unapproved"
              SessionLength = match gci remoteSession.CategoryItems "Talk length" with
                              | Some item -> item.Name
                              | None -> ""
              Track = match gci remoteSession.CategoryItems "Track Type" with
                      | Some item -> item.Name
                      | None -> ""
              Topic = match gqa remoteSession.QuestionAnswers "Topics" with
                      | Some a -> a.AnswerValue
                      | None -> "" }

        session

    let addNewSessions (log: ILogger) (remoteSessions: array<SessionV2>) (existingSessions: seq<SessionV2>) table =
        let newSessions = remoteSessions
                            |> Array.filter (fun rs ->
                                let m = existingSessions
                                        |> Seq.filter (fun es -> es.SessionizeId = rs.SessionizeId)
                                match Seq.length m with
                                | 0 -> true
                                | _ -> false
                                )

        log.LogInformation(
            sprintf "Found %d new sessions" (Array.length newSessions)
        )

        newSessions
        |> Seq.map Insert
        |> autobatch
        |> List.map (inTableToClientAsBatch table)

    let updateSessions (log: ILogger) (remoteSessions: array<SessionV2>) (existingSessions: seq<SessionV2>) table =
        let updatableSessions = existingSessions
                                |> Seq.filter (fun es ->
                                    let m = remoteSessions
                                            |> Array.filter (fun rs -> rs.SessionizeId = es.SessionizeId)
                                    match Array.length m with
                                    | 0 -> false
                                    | _ -> true
                                    )
        log.LogInformation(
            sprintf "Found %d updatable sessions" (Seq.length updatableSessions)
        )

        updatableSessions
        |> Seq.map InsertOrMerge
        |> autobatch
        |> List.map (inTableToClientAsBatch table)

    let addNewSpeakers (log: ILogger) (remoteSpeakers: array<Presenter>) (existingSpeakers: seq<Presenter>) table =
        let newSpeakers = remoteSpeakers
                            |> Array.filter (fun rs ->
                                let m = existingSpeakers
                                        |> Seq.filter (fun es -> es.TalkId = rs.TalkId && es.Id = rs.Id)
                                match Seq.length m with
                                | 0 -> true
                                | _ -> false
                                )

        log.LogInformation(
            sprintf "Found %d new speakers" (Array.length newSpeakers)
        )

        newSpeakers
        |> Seq.map Insert
        |> autobatch
        |> List.map (inTableToClientAsBatch table)

    let updateSpeakers (log: ILogger) (remoteSpeakers: array<Presenter>) (existingSpeakers: seq<Presenter>) table =
        let updatableSpeakers = remoteSpeakers
                                |> Array.filter (fun rs ->
                                    let m = existingSpeakers
                                            |> Seq.filter (fun es -> es.TalkId = rs.TalkId && es.Id = rs.Id)
                                    match Seq.length m with
                                    | 0 -> false
                                    | _ -> true
                                    )

        log.LogInformation(
            sprintf "Found %d updatable speakers" (Array.length updatableSpeakers)
        )

        updatableSpeakers
        |> Seq.map InsertOrMerge
        |> autobatch
        |> List.map (inTableToClientAsBatch table)
