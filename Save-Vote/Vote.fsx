open System

[<CLIMutable>]
type Vote = {
    PartitionKey: string
    RowKey: string
    IpAddress: string
    SessionId: Guid
    SubmittedDateUTC: DateTimeOffset
}