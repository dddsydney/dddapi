#load "../SharedCode/SessionTableEntity.fsx"
#load "../SharedCode/Sessionize.fsx"

#r "Microsoft.WindowsAzure.Storage"

open SessionTableEntity
open Sessionize

open System
open Microsoft.WindowsAzure.Storage.Table

let findSpeakers (speakers: array<Guid>) (allSpeakers: array<Speaker>) =
    allSpeakers
    |> Array.filter (fun x -> speakers |> Array.exists (fun s -> x.Id = s))

let getSpeakerLink (title: string) (speaker: Speaker) =
    match (speaker.Links |> Array.exists (fun link -> link.Title = title)) with
    | true ->
        let link = speaker.Links |> Array.find (fun link -> link.Title = title)
        Some link.Url
    | _ -> None
    
let getCategoryItem (title: string) (id: int) (categories: array<SessionizeCategory>) =
    match categories |> Array.exists (fun c -> c.Title = title) with
    | true ->
        let category = categories |> Array.find (fun c -> c.Title = title)
        let item = category.Items |> Array.find (fun i -> i.Id = id)
        Some (item.Name)
    | _ -> None

let makeSession (allSpeakers: array<Speaker>) (categories: array<SessionizeCategory>) (remoteSession: SubmittedSession) =
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

    match getCategoryItem "Level" (remoteSession.CategoryItems.[0]) categories with
    | Some name ->
        session.RecommendedAudience <- name
        ignore
    | None -> ignore

    session.PresenterName <- speakerNames
    session.PresenterEmail <- ""
    
    match getSpeakerLink "Twitter" firstSpeaker with
    | Some url ->
        session.PresenterTwitterAlias <- url
        ignore
    | None -> ignore

    match getSpeakerLink "Blog" firstSpeaker with
    | Some url ->
        session.PresenterWebsite <- url
        ignore
    | None -> ignore

    session.PresenterBio <- firstSpeaker.Bio
    session.SubmittedDateUtc <- DateTime.Now
    session.Timestamp <- DateTimeOffset.Now
    session.Status <- 1

    session
