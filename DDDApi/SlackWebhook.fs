module SlackWebhook

open FSharp.Data
open DDDApi
open Newtonsoft.Json
open FSharp.Data.HttpRequestHeaders
open Newtonsoft.Json.Serialization

type WebhookField =
    { Title: string
      Value: string
      Short: bool }

type WebhookAttachment =
    { Fallback: string
      Pretext: string
      Color: string
      Fields: WebhookField array }

type WebhookMessage =
    { Attachments: WebhookAttachment array }

let notifySessionsAdded url session presenters =
    let presenterNames =
        presenters
        |> Seq.map (fun p -> p.FullName)
        |> String.concat ", "

    let msg =
        { Attachments = 
            [|{ Fallback = sprintf "(%s) '%s' by %s" session.SessionizeId session.Title presenterNames
                Pretext = sprintf "(%s) _%s_ by *%s*" session.SessionizeId session.Title presenterNames
                Color = "good"
                Fields =
                [|{ Title = "Track"
                    Value = session.Track
                    Short = true }
                  { Title = "Length"
                    Value = session.SessionLength
                    Short = true }
                  { Title = "Topic"
                    Value = session.Topic
                    Short = false }|] } |] }

    let settings = JsonSerializerSettings()
    settings.ContractResolver <- CamelCasePropertyNamesContractResolver()

    let submitMessage = JsonConvert.SerializeObject(msg, settings)

    Http.AsyncRequestString
        ( url,
          headers = [ ContentType HttpContentTypes.Json ],
          body = TextRequest submitMessage )