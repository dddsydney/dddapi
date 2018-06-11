namespace DDDApi.Functions

open Microsoft.Extensions.Configuration
open Microsoft.Azure.WebJobs
open Microsoft.WindowsAzure.Storage.Table
open DDDApi
open DDDApi.SessionizeApi
open Microsoft.Azure.WebJobs.Host
open FSharp.Azure.Storage.Table

module SessionizeFunctions =
    [<FunctionName("Download_Sessionzie_data")>]
    let downloadSessionize([<TimerTrigger("0 5 * * * *")>]timer: TimerInfo,
                           [<Table("Session")>]sessionsSource: CloudTable,
                           context: ExecutionContext,
                           log: TraceWriter) =
        let config = (new ConfigurationBuilder())
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", true, true)
                        .AddEnvironmentVariables()
                        .Build()

        let apiKey = config.["Sessionize.ApiKey"]
        async {
            let! sessionize = SessionizeApi.downloadSessionize apiKey

            let makeSession' = SessionizeApi.makeSession sessionize.Speakers sessionize.Categories

            let remoteSessions = sessionize.Sessions
                                |> Array.map makeSession'

            let existingSessions = Query.all<Session>
                                   |> Query.where <@ fun _ s -> s.PartitionKey = "Session-2018" @>
                                   |> fromTable sessionsSource.ServiceClient sessionsSource.Name
                                   |> Seq.map(fun (s, _) -> s)


            (addNewSessions log remoteSessions existingSessions sessionsSource) |> ignore
            (updateSessions log remoteSessions existingSessions sessionsSource) |> ignore

            log.Info("Writing to queue")

            return ignore
        } |> Async.StartAsTask