open System

type SubmittedSession() =
    member val Id: int = 0 with get, set
    member val Title: string = null with get, set
    member val Description: string = null with get, set
    member val Speakers: array<Guid> = Array.empty with get, set
    member val CategoryItems: array<int> = Array.empty with get, set

type SpeakerLinks() =
    member val Title: string = null with get, set
    member val Url: string = null with get, set

type Speaker() =
    member val Id: Guid = Guid.Empty with get, set
    member val FirstName: string = null with get, set
    member val LastName: string = null with get, set
    member val Bio: string = null with get, set
    member val TagLine: string = null with get, set
    member val ProfilePicture: string = null with get, set
    member val Links: array<SpeakerLinks> = Array.empty with get, set
    member val Sessions: array<int> = Array.empty with get, set
    member val FullName: string = null with get, set
    
type SessionLevel() =
    member val Id: int = 0 with get, set
    member val Name: string = null with get, set

type SessionizeCategory() =
    member val Id: int = 0 with get, set
    member val Title: string = null with get, set
    member val Items: array<SessionLevel> = Array.empty with get, set

type Sessionize() =
    member val Sessions: array<SubmittedSession> = Array.empty with get, set
    member val Speakers: array<Speaker> = Array.empty with get, set
    member val Categories: array<SessionizeCategory> = Array.empty with get, set