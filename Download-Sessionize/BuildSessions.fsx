#load "../SharedCode/SessionTableEntity.fsx"
#load "../SharedCode/Sessionize.fsx"

#r "Microsoft.WindowsAzure.Storage"

open SessionTableEntity
open Sessionize

open System
open Microsoft.WindowsAzure.Storage.Table

module Array =
    let public findOrNone matcher items =
        match items |> Array.exists matcher with
        | true -> Some(items |> Array.find matcher)
        | _ -> None

let findSpeakers (speakers: array<Guid>) (allSpeakers: array<Speaker>) =
    allSpeakers
    |> Array.filter (fun x -> speakers |> Array.exists (fun s -> x.Id = s))

let getSpeakerLink (title: string) (speaker: Speaker) =
    speaker.Links |> Array.findOrNone (fun link -> link.Title = title)

let getCategoryItem (title: string) (items: array<int>) (categories: array<SessionizeCategory>) =
    match categories |> Array.findOrNone (fun c -> c.Title = title) with
    | Some category -> category.Items |> Array.findOrNone (fun ci -> items |> Array.exists(fun i -> i = ci.Id))
    | None -> None

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
        session.PresenterTwitterAlias <- link.Url.Replace("https://twitter.com/", "")
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
