open System

type VotePeriod =
    { Start: DateTimeOffset
      End: DateTimeOffset }

let AESTOffset = TimeSpan.FromHours(8.0)

let Voting2018 =
                { Start = DateTimeOffset(2018, 06, 14, 08, 00, 00, 00, AESTOffset);
                  End = DateTimeOffset(2018, 06, 26, 23, 59, 00, 00, AESTOffset) }

let validVotingPeriod now year =
    match year with
    | "2018" ->
        now >= Voting2018.Start && now <= Voting2018.End
    | _ -> false
