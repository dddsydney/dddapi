namespace DDDApi
open FSharp.Azure.Storage.Table
open Microsoft.WindowsAzure.Storage.Table

module azureTableUtils =
    let fromTableToClientAsync (table: CloudTable) q = fromTableAsync table.ServiceClient table.Name q
    let fromTableToClient (table: CloudTable) q = fromTable table.ServiceClient table.Name q

    let inTableToClientAsync (table: CloudTable) o = inTableAsync table.ServiceClient table.Name o
    let inTableToClient (table: CloudTable) o = inTable table.ServiceClient table.Name o

    let inTableToClientAsBatch (table: CloudTable) o = inTableAsBatch table.ServiceClient table.Name o