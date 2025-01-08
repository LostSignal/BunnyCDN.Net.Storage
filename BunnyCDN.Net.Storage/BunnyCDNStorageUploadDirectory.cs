using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BunnyCDN.Net.Storage
{
    public partial class BunnyCDNStorage
    {
        private const string ManifestFileName = "manifest.json";

        public async Task UploadLocalDirectory(string directoryToUpload, string destinationFolder = null)
        {
            this.Log($"Generating Local Manifest...");
            var localManifest = this.GenerateLocalManifest(directoryToUpload);

            this.Log($"Downloading Remote Manifest...");
            var remoteManifest = await this.GetRemoteManifest(destinationFolder);

            bool didFilesChange = false;

            // Adds and Updates
            foreach (var fileHashPair in localManifest)
            {
                string relativePath = fileHashPair.Key;
                string localSHA256Hash = fileHashPair.Value;

                // If it doesn't exist on the server, or the Sha256 Hashes don't match
                if (remoteManifest.TryGetValue(relativePath, out string remoteSHA256Hash) == false || localSHA256Hash != remoteSHA256Hash)
                {
                    string localFilePath = Path.Combine(directoryToUpload, relativePath).Replace("\\", "/");
                    string storagePath = this.CalculateDestinationPath(this.StorageZoneName, destinationFolder, relativePath);

                    this.Log($"Uploading: {storagePath}");
                    await this.UploadAsync(localFilePath, storagePath, localSHA256Hash);
                    didFilesChange = true;
                }
            }

            // Removes
            foreach (var remoteHashPair in remoteManifest)
            {
                string relativePath = remoteHashPair.Key;

                if (localManifest.ContainsKey(relativePath) == false)
                {
                    string storagePath = this.CalculateDestinationPath(this.StorageZoneName, destinationFolder, relativePath);

                    this.Log($"Deleting: {storagePath}");
                    await this.DeleteObjectAsync(storagePath);
                    didFilesChange = true;
                }
            }

            // Saving the manifest file
            if (didFilesChange)
            {
                var manifestJson = Serializer.Serialize<Dictionary<string, string>>(localManifest);
                var manifestJsonBytes = Encoding.ASCII.GetBytes(manifestJson);
                var manifestJsonSha256Hash = this.GenerateSha256HashFromBytes(manifestJsonBytes);
                var manifestJsonStoragePath = this.CalculateDestinationPath(this.StorageZoneName, destinationFolder, ManifestFileName);

                using (var manifestJsonStream = new MemoryStream(manifestJsonBytes))
                {
                    this.Log("Saving Manifest...");
                    await this.UploadAsync(manifestJsonStream, manifestJsonStoragePath, manifestJsonSha256Hash);
                }
            }
            else
            {
                this.Log("No Changes Detected...");
            }
        }

        private string GenerateSha256Hash(string filePath) => this.GenerateSha256HashFromBytes(File.ReadAllBytes(filePath));

        private string GenerateSha256HashFromBytes(byte[] bytes)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] checksumData = sha.ComputeHash(bytes);
                return BitConverter.ToString(checksumData).Replace("-", String.Empty);
            }
        }

        private Dictionary<string, string> GenerateLocalManifest(string directoryToUpload)
        {
            var result = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(directoryToUpload, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(directoryToUpload.Length).Replace("\\", "/");
                relativePath = relativePath.StartsWith("/") ? relativePath.Substring(1) : relativePath;

                result.Add(relativePath, this.GenerateSha256Hash(file));
            }

            return result;
        }

        private string CalculateDestinationPath(params string[] strings) =>
            this.NormalizePath(string.Join("/", strings.Where(x => string.IsNullOrWhiteSpace(x) == false)));

        private async Task<Dictionary<string, string>> GetRemoteManifest(string destinationFolder)
        {
            try
            {
                var manifestJsonStoragePath = this.CalculateDestinationPath(this.StorageZoneName, destinationFolder, ManifestFileName);
                var stream = await this.DownloadObjectAsStreamAsync(manifestJsonStoragePath);

                using (var streamReader = new StreamReader(stream))
                {
                    return Serializer.Deserialize<Dictionary<string, string>>(streamReader.ReadToEnd());
                }
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private void Log(string message)
        {
#if !UNITY
            Console.WriteLine(message);
#else
            UnityEngine.Debug.Log(message);
#endif
        }
    }
}
