#load "../SharedCode/SessionTableEntity.fsx"
#load "../SharedCode/Sessionize.fsx"
#load "BuildSessions.fsx"

#r "System.Net.Http"
#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

open SessionTableEntity
open Sessionize
open BuildSessions

open System
open System.Configuration
open System.Linq
open System.Net
open System.Net.Http
open Microsoft.WindowsAzure.Storage.Table
open Newtonsoft.Json

let downloadSessionize() =
    use client = HttpClient()
    
    let sessionizeApiKey = ConfigurationManager.AppSettings.["SessionizeApiKey"]
    
    let url = sprintf "https://sessionize.com/api/v2/%s/view/all" sessionizeApiKey

    let response = url
                    |> client.GetAsync
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

    response.Content.ReadAsStringAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously

let addNewSessions (log: TraceWriter) (remoteSessions: array<Session>) (existingSessions: IQueryable<Session>) (table: ICollector<Session>) =
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

    newSessions |> Array.iter (fun s -> table.Add s)
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

let Run(timer: TimerInfo, sessionsSource: IQueryable<Session>, sessionsDest: ICollector<Session>, table: CloudTable, log: TraceWriter) =
    log.Info(
        sprintf "Starting to download from Sessionize at: %s" 
            (DateTime.Now.ToString()))

    let content = downloadSessionize()

    let sessionize = JsonConvert.DeserializeObject<Sessionize>(content)
    
    let makeSession' = makeSession sessionize.Speakers sessionize.Categories
    
    let remoteSessions = sessionize.Sessions
                        |> Array.map makeSession'

    let existingSessions = query {
        for session in sessionsSource do
        where (session.PartitionKey = "Session-2018")
        select session
    }

    addNewSessions log remoteSessions existingSessions sessionsDest
    updateSessions log remoteSessions existingSessions table

    log.Info("Writing to queue")
    
    content
