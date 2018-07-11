namespace DDDApi.Functions

open DDDApi
open DDDApi.azureTableUtils

open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.WindowsAzure.Storage.Table
open FSharp.Azure.Storage.Table
open System

[<CLIMutable>]
type Agenda = {
    PartitionKey: string
    RowKey: string
    SessionId: Guid
    RoomName: string
    RoomColour: string
    ServiceSession: bool
    ServiceSessionName: string
    Timeslot: string
}

type AgendaResponseItem =
     { Title: string
       Timeslot: string
       Description: string
       PresenterName: string
       PresenterWebsite: string
       PresenterTwitter: string
       PresenterBio: string
       RoomName: string
       RoomColour: string }

module AgendaFunctions =
    let processAgenda (agendaItems: seq<Agenda>) (sessions: seq<Session>) =
        let agendaResponse = agendaItems
                             |> Seq.map(fun (agenda) ->
                                        match agenda.ServiceSession with
                                        | true ->
                                                  { Title = agenda.ServiceSessionName
                                                    Description = ""
                                                    RoomName = agenda.RoomName
                                                    RoomColour = agenda.RoomColour
                                                    Timeslot = agenda.Timeslot 
                                                    PresenterName = ""
                                                    PresenterWebsite = ""
                                                    PresenterTwitter = ""
                                                    PresenterBio = "" }
                                        | false ->
                                                  let session = sessions
                                                                |> Seq.find(fun s -> Guid(s.RowKey) = agenda.SessionId)
                                                  { Title = session.SessionTitle
                                                    Description = session.SessionAbstract
                                                    RoomName = agenda.RoomName
                                                    RoomColour = agenda.RoomColour
                                                    Timeslot = agenda.Timeslot 
                                                    PresenterName = session.PresenterName
                                                    PresenterWebsite = session.PresenterWebsite
                                                    PresenterTwitter = session.PresenterTwitterAlias
                                                    PresenterBio = session.PresenterBio })

        OkObjectResult(agendaResponse) :> IActionResult

    [<FunctionName("Get_agenda_for_year")>]
    let getAgenda([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Get-Agenda/{year}")>] req: HttpRequest,
                    [<Table("Sessions")>]sessionsTable: CloudTable,
                    [<Table("Agenda")>]agendaTable: CloudTable,
                    year: string) =
        async {
            let! agendaItems = Query.all<Agenda>
                               |> Query.where<@ fun _ s -> s.PartitionKey = year @>
                               |> fromTableToClientAsync agendaTable

            let processAgenda' = agendaItems |> Seq.map(fun (a, _) -> a) |> processAgenda

            return! match Seq.length agendaItems with
                    | 0 -> async { return NotFoundObjectResult() :> IActionResult }
                    | _ -> async {
                        let sessionIds = agendaItems
                                         |> Seq.filter(fun (a, _) -> not (isNull (a.SessionId.ToString())))
                                         |> Seq.map(fun (a, _) -> a.SessionId.ToString())
                        let! sessions = Query.all<Session>
                                        |> Query.where<@ fun _ s -> sessionIds |> Seq.contains s.RowKey @>
                                        |> fromTableToClientAsync sessionsTable

                        return sessions |> Seq.map(fun (x, _) -> x) |> processAgenda'
                   }
        } |> Async.StartAsTask
