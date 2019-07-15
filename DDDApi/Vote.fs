module Voting
open System
open FSharp.Azure.Storage.Table

[<CLIMutable>]
type Vote =
    { [<PartitionKey>]Year: string
      [<RowKey>]VoteId: string
      IpAddress: string
      SessionId: string
      SubmittedDateUTC: DateTimeOffset
      TicketNumber: string
      VoterSessionId: string
      Id: string
      VotingStartTime: string }

type VotePeriod =
    { Start: DateTimeOffset
      End: DateTimeOffset }

let AESTOffset = TimeSpan.FromHours(10.0)

let Voting2018 =
    { Start = DateTimeOffset(2018, 06, 14, 08, 00, 00, 00, AESTOffset);
      End = DateTimeOffset(2018, 06, 26, 23, 59, 00, 00, AESTOffset) }

let Voting2019 =
    { Start = DateTimeOffset(2019, 07, 15, 08, 00, 00, 00, AESTOffset);
        End = DateTimeOffset(2019, 07, 28, 23, 59, 00, 00, AESTOffset) }

let validVotingPeriod now year =
    match year with
    | "2018" ->
        now >= Voting2018.Start && now <= Voting2018.End
    | "2019" ->
        now >= Voting2019.Start && now <= Voting2019.End
    | _ -> false
