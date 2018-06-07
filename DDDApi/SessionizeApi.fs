namespace DDDApi

open FSharp.Data
open System
open Microsoft.Azure.WebJobs.Host
open System.Linq
open Microsoft.Azure.WebJobs
open Microsoft.WindowsAzure.Storage.Table

module SessionizeApi =
    type Sessionize = JsonProvider<"../sessionize-sample.json">
    
    let downloadSessionize apiKey =
        Sessionize.AsyncLoad (sprintf "https://sessionize.com/api/v2/%s/view/all" apiKey)

    let findSpeakers (speakers: array<Guid>) (allSpeakers: array<Sessionize.Speaker>) =
        allSpeakers
        |> Array.filter (fun x -> speakers |> Array.exists (fun s -> x.Id = s))

    let getSpeakerLink (title: string) (speaker: Sessionize.Speaker) =
        speaker.Links |> Array.findOrNone (fun link -> link.Title = title)

    let getCategoryItem (title: string) (items: array<int>) (categories: array<Sessionize.Category>) =
        match categories |> Array.findOrNone (fun c -> c.Title = title) with
        | Some category -> category.Items |> Array.findOrNone (fun ci -> items |> Array.exists(fun i -> i = ci.Id))
        | None -> None

    let makeSession (allSpeakers: array<Sessionize.Speaker>) (categories: array<Sessionize.Category>) (remoteSession: Sessionize.Session) =
        let speakers = findSpeakers (remoteSession.Speakers) allSpeakers
        let speakerNames = speakers
                           |> Array.map (fun s -> s.FullName)
                           |> Array.reduce (fun x y -> x + ", " + y)

        let firstSpeaker = Array.get speakers 0

        let session = Session()
        session.PartitionKey <- "Session-2018"
        session.RowKey <- Guid.NewGuid().ToString()
        session.RemoteId <- remoteSession.Id
        session.SessionTitle <- remoteSession.Title
        session.SessionAbstract <- remoteSession.Description

        (match getCategoryItem "Level" (remoteSession.CategoryItems) categories with
        | Some item ->
            session.RecommendedAudience <- item.Name
            ignore
        | None -> ignore) |> ignore

        (match getCategoryItem "Track Type" (remoteSession.CategoryItems) categories with
        | Some item ->
            session.TrackType <- item.Name
            ignore
        | None -> ignore) |> ignore

        (match getCategoryItem "Talk length" (remoteSession.CategoryItems) categories with
        | Some item ->
            session.SessionLength <- item.Name
            ignore
        | None -> ignore) |> ignore

        session.PresenterName <- speakerNames
        session.PresenterEmail <- ""

        (match getSpeakerLink "Twitter" firstSpeaker with
        | Some link ->
            session.PresenterTwitterAlias <- link.Url
            ignore
        | None -> ignore) |> ignore

        (match getSpeakerLink "Blog" firstSpeaker with
        | Some link ->
            session.PresenterWebsite <- link.Url
            ignore
        | None -> ignore) |> ignore

        session.PresenterBio <- firstSpeaker.Bio
        session.SubmittedDateUtc <- DateTime.Now
        session.Timestamp <- DateTimeOffset.Now
        session.Status <- 1

        session

    let addNewSessions (log: TraceWriter) (remoteSessions: array<Session>) (existingSessions: IQueryable<Session>) (table: CloudTable) =
        let newSessions = remoteSessions
                            |> Array.filter (fun rs ->
                                let m = existingSessions
                                        |> Seq.filter (fun es -> es.RemoteId = rs.RemoteId)
                                match Seq.length m with
                                | 0 -> true
                                | _ -> false
                                )

        log.Info(
            sprintf "Found %d new sessions" (Array.length newSessions)
        )

        // newSessions |> Array.iter table.CreateAsync
        newSessions

    let updateSessions (log: TraceWriter) (remoteSessions: array<Session>) (existingSessions: IQueryable<Session>) (table: CloudTable) =
        let updatableSessions = existingSessions
                                |> Seq.filter (fun es ->
                                    let m = remoteSessions
                                            |> Array.filter (fun rs -> rs.RemoteId = es.RemoteId)
                                    match Array.length m with
                                    | 0 -> false
                                    | _ -> true
                                    )
        log.Info(
            sprintf "Found %d updatable sessions" (Seq.length updatableSessions)
        )

        updatableSessions |> Seq.iter (fun s -> 
                            let remoteSession = remoteSessions |> Array.find (fun rs -> rs.RemoteId = s.RemoteId)
                            s.SessionTitle <- remoteSession.SessionTitle
                            s.SessionAbstract <- remoteSession.SessionAbstract
                            s.RecommendedAudience <- remoteSession.RecommendedAudience
                            s.PresenterName <- remoteSession.PresenterName
                            s.PresenterTwitterAlias <- remoteSession.PresenterTwitterAlias
                            s.PresenterWebsite <- remoteSession.PresenterWebsite
                            s.PresenterBio <- remoteSession.PresenterBio
                            s.Timestamp <- DateTimeOffset.Now

                            let op = TableOperation.Replace(s)
                            table.ExecuteAsync(op)
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                            |> ignore
                            )
        updatableSessions