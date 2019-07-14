namespace DDDApi.Functions

open Microsoft.Extensions.Configuration
open Microsoft.Azure.WebJobs
open Microsoft.WindowsAzure.Storage.Table
open DDDApi
open DDDApi.SessionizeApi
open FSharp.Azure.Storage.Table
open Microsoft.Extensions.Logging
open SlackWebhook

module SessionizeFunctions =
    [<FunctionName("Download_Sessionzie_data")>]
    let downloadSessionize ([<TimerTrigger("0 0 * * * *")>] timer: TimerInfo)
                           ([<Table("Session", Connection = "EventStorage")>] sessionsSource: CloudTable)
                           ([<Table("Presenter", Connection = "EventStorage")>] speakersSource: CloudTable)
                           (context: ExecutionContext)
                           (log: ILogger) =
        ignore()
        //let config = (ConfigurationBuilder())
        //                .SetBasePath(context.FunctionAppDirectory)
        //                .AddJsonFile("local.settings.json", true, true)
        //                .AddEnvironmentVariables()
        //                .Build()

        //let apiKey = config.["SessionizeApiKey"]
        //async {
        //    let! sessionize = downloadSessionize apiKey

        //    let makeSession' = makeSession sessionize.Categories sessionize.Questions

        //    let remoteSessions = sessionize.Sessions
        //                                     |> Array.map makeSession'

        //    let existingSessions = Query.all<SessionV2>
        //                           |> Query.where <@ fun s _ -> s.EventYear = "2019" @>
        //                           |> fromTableToClient sessionsSource
        //                           |> Seq.map(fun (s, _) -> s)

        //    let newSessions = addNewSessions log remoteSessions existingSessions sessionsSource
        //    let _ = updateSessions log remoteSessions existingSessions sessionsSource

        //    let existingSpeakers = Query.all<Presenter>
        //                           |> Query.where<@ fun s _ -> s.EventYear = "2019" @>
        //                           |> fromTableToClient speakersSource
        //                           |> Seq.map(fun (s, _) -> s)

        //    let remoteSpeakers = remoteSessions
        //                         |> Array.map (makeSpeakers sessionize.Speakers sessionize.Categories sessionize.Questions)
        //                         |> Array.concat

        //    let _ = addNewSpeakers log remoteSpeakers existingSpeakers speakersSource
        //    let _ = updateSpeakers log remoteSpeakers existingSpeakers speakersSource

        //    match Array.length newSessions with
        //    | 0 -> return ignore()
        //    | _ ->
        //        let n = notifySessionsAdded config.["NewSessionNotificationWebhook"]
        //        let! _ = newSessions
        //                 |> Array.map (fun session ->
        //                    let presenters = remoteSpeakers |> Array.filter(fun presenter -> presenter.TalkId = session.SessionizeId)
        //                    n session presenters)
        //                 |> Async.Parallel

        //        return ignore()
        //}
        //|> Async.StartAsTask
