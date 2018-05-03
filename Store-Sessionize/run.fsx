#r "Microsoft.WindowsAzure.Storage"

open System
open System.Configuration
open System.Text
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

let GetBlob (storageAcc:string) containerName blobName =
    let connString = ConfigurationManager.AppSettings.[storageAcc]
    let storageAccount = CloudStorageAccount.Parse(connString)
    let blobClient = storageAccount.CreateCloudBlobClient()
    let container = blobClient.GetContainerReference(containerName)
    container.GetBlockBlobReference(blobName)

let Run(inputMessage: string, log: TraceWriter) =
    let filename = sprintf "%s.json" (DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"))

    log.Info(
        sprintf "Preparing to store some sessionize data: %s" filename
    )
        
    let cloudBlockBlob = GetBlob "DDDSydney_Storage" "sessionize-captures" filename

    let bytes = Encoding.UTF8.GetBytes(inputMessage)
    cloudBlockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length).Wait()
    cloudBlockBlob.SetMetadata()
    cloudBlockBlob.Properties.ContentType <- "application/json"
    cloudBlockBlob.SetProperties()