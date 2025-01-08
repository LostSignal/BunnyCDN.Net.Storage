
using ConsoleAppFramework;

var consoleApp = ConsoleApp.Create();

consoleApp.Add("", (string storageZone, string apiAccessKey, string mainReplicationRegion, string directory) =>
{
    var storage = new BunnyCDN.Net.Storage.BunnyCDNStorage(storageZone, apiAccessKey, mainReplicationRegion);
    storage.UploadLocalDirectory(directory).Wait();
});

consoleApp.Run(args);
