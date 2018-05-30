#r "Microsoft.WindowsAzure.Storage"

open System
open Microsoft.WindowsAzure.Storage.Table

type Session() =
    inherit TableEntity()
    member val SessionTitle: string = null with get, set
    member val SessionAbstract: string = null with get, set
    member val RecommendedAudience: string = null with get, set
    member val PresenterName: string = null with get, set
    member val PresenterEmail: string = null with get, set
    // member val PresenterMobileNumber: string = null with get, set
    member val PresenterTwitterAlias: string = null with get, set
    member val PresenterWebsite: string = null with get, set
    member val PresenterBio: string = null with get, set
    member val SubmittedDateUtc: DateTime = DateTime.MinValue with get, set
    member val Status: int = 0 with get, set
    member val RemoteId: int = 0 with get, set
    member val SessionLength: string = null with get, set
    member val TrackType: string = null with get, set
    // member val SubmitterIp: string = null with get, set
