namespace DDDApi
open FSharp.Azure.Storage.Table
open Microsoft.WindowsAzure.Storage.Table

module azureTableUtils =
    let fromTableToClient (table: CloudTable) q = fromTable table.ServiceClient table.Name q
    let inTableToClient (table: CloudTable) o = inTable table.ServiceClient table.Name o