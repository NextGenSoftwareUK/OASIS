using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract partial class STARNETManagerBase<T1, T2, T3, T4>
    {

        //public virtual async Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        public virtual async Task<OASISResult<bool>> UploadToCloudAsync(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Uploading });
                StorageClient storage = await StorageClient.CreateAsync();
                //var bucket = storage.CreateBucket("oasis", "STARNETHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARNETDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    await storage.UploadObjectAsync(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");
                result.Result = true;

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARNETDNA.PublishedPath;

                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //using (var content = new MultipartFormDataContent())
                //{
                //    content.Remove(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                //    client.DefaultRequestHeaders.Remove("pinata_api_key", pinataApiKey);
                //    client.DefaultRequestHeaders.Remove("pinata_secret_api_key", pinataSecretApiKey);

                //    var response = await client.PostAsync(pinataUrl, content);
                //    response.EnsureSuccessStatusCode();

                //    var responseBody = await response.Content.ReadAsStringAsync();
                //    //return responseBody;
                //}


                //                           var config = new Config
                //                           {
                //                               ApiKey = "33e4469830a51af0171b",
                //                               ApiSecret = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs"
                //                           };

                //                           Pinata.Client.PinataClient pinClient = new Pinata.Client.PinataClient(config);

                //                           //var html = @"
                //                           //    <html>
                //                           //       <head>
                //                           //          <title>Hello IPFS!</title>
                //                           //       </head>
                //                           //       <body>
                //                           //          <h1>Hello World</h1>
                //                           //       </body>
                //                           //    </html>
                //                           //    ";

                //                           var metadata = new PinataMetadata // optional
                //                           {
                //                               KeyValues =
                //{
                //   {"Author", "David Ellams"}
                //}
                //                           };

                //                           var options = new PinataOptions(); // optional

                //                           options.CustomPinPolicy.RemoveOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                //                           using (var content = new MultipartFormDataContent())
                //                           {
                //                               var fileContent = new ByteArrayContent(fileBytes);
                //                               content.Remove(fileContent, "file", Path.GetFileName(filePath));
                //                           }

                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                //                           {
                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                //                               content.RemovePinataFile(file, "index.html");
                //                           },
                //                              metadata,
                //                              options);

                //                           if (response.IsSuccess)
                //                           {
                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                //                           }

                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARNETDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARNETDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = true;
                //    STARNETDNA.STARNETHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARNETHolonUIName} to cloud storage. Reason: {e}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARNETDNA.PublishedToCloud = false;
            }

            return result;
        }

        public OASISResult<bool> UploadToCloud(T4 STARNETDNA, string publishedSTARNETHolonFileName, bool registerOnSTARNET, ProviderType binaryProviderType)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = STARNETDNA, Status = STARNETHolonPublishStatus.Uploading });
                StorageClient storage = StorageClient.Create();
                //var bucket = storage.CreateBucket("oasis", "STARNETHolons");

                // set minimum chunksize just to see progress updating
                var uploadObjectOptions = new UploadObjectOptions
                {
                    ChunkSize = UploadObjectOptions.MinimumChunkSize
                };

                var progressReporter = new Progress<Google.Apis.Upload.IUploadProgress>(OnUploadProgress);
                using (var fileStream = File.OpenRead(STARNETDNA.PublishedPath))
                {
                    _fileLength = fileStream.Length;
                    _progress = 0;

                    storage.UploadObject(STARNETHolonGoogleBucket, publishedSTARNETHolonFileName, "", fileStream, uploadObjectOptions, progress: progressReporter);
                }

                _progress = 100;
                OnUploadStatusChanged?.Invoke(this, new STARNETHolonUploadProgressEventArgs() { Progress = _progress, Status = STARNETHolonUploadStatus.Uploading });
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");
                result.Result = true;

                //HttpClient client = new HttpClient();
                //string pinataApiKey = "33e4469830a51af0171b";
                //string pinataSecretApiKey = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs";
                //string pinataUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";
                //string filePath = STARNETDNA.PublishedPath;

                //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //using (var content = new MultipartFormDataContent())
                //{
                //    content.Remove(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
                //    client.DefaultRequestHeaders.Remove("pinata_api_key", pinataApiKey);
                //    client.DefaultRequestHeaders.Remove("pinata_secret_api_key", pinataSecretApiKey);

                //    var response = await client.PostAsync(pinataUrl, content);
                //    response.EnsureSuccessStatusCode();

                //    var responseBody = await response.Content.ReadAsStringAsync();
                //    //return responseBody;
                //}


                //                           var config = new Config
                //                           {
                //                               ApiKey = "33e4469830a51af0171b",
                //                               ApiSecret = "ff57367b2b125bf5f06f79b30b466890c84eed101c12af064459d88d8bb8d8a0\r\nJWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiIzMGI3NjllNS1hMjJmLTQxN2UtOWEwYi1mZTQ2NzE5MjgzNzgiLCJlbWFpbCI6ImRhdmlkZWxsYW1zQGhvdG1haWwuY29tIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsInBpbl9wb2xpY3kiOnsicmVnaW9ucyI6W3siZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiRlJBMSJ9LHsiZGVzaXJlZFJlcGxpY2F0aW9uQ291bnQiOjEsImlkIjoiTllDMSJ9XSwidmVyc2lvbiI6MX0sIm1mYV9lbmFibGVkIjpmYWxzZSwic3RhdHVzIjoiQUNUSVZFIn0sImF1dGhlbnRpY2F0aW9uVHlwZSI6InNjb3BlZEtleSIsInNjb3BlZEtleUtleSI6IjMzZTQ0Njk4MzBhNTFhZjAxNzFiIiwic2NvcGVkS2V5U2VjcmV0IjoiZmY1NzM2N2IyYjEyNWJmNWYwNmY3OWIzMGI0NjY4OTBjODRlZWQxMDFjMTJhZjA2NDQ1OWQ4OGQ4YmI4ZDhhMCIsImV4cCI6MTc3Mzc4NDAzNX0.L-6_BPMsvhN3Es72Q5lZAFKpBEDF9kEibOGdWd_PxHs"
                //                           };

                //                           Pinata.Client.PinataClient pinClient = new Pinata.Client.PinataClient(config);

                //                           //var html = @"
                //                           //    <html>
                //                           //       <head>
                //                           //          <title>Hello IPFS!</title>
                //                           //       </head>
                //                           //       <body>
                //                           //          <h1>Hello World</h1>
                //                           //       </body>
                //                           //    </html>
                //                           //    ";

                //                           var metadata = new PinataMetadata // optional
                //                           {
                //                               KeyValues =
                //{
                //   {"Author", "David Ellams"}
                //}
                //                           };

                //                           var options = new PinataOptions(); // optional

                //                           options.CustomPinPolicy.RemoveOrUpdateRegion("NYC1", desiredReplicationCount: 1);

                //                           //var response = await client.Pinning.PinFileToIpfsAsync()

                //                           byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                //                           using (var content = new MultipartFormDataContent())
                //                           {
                //                               var fileContent = new ByteArrayContent(fileBytes);
                //                               content.Remove(fileContent, "file", Path.GetFileName(filePath));
                //                           }

                //                           var response = await pinClient.Pinning.PinFileToIpfsAsync(content =>
                //                           {
                //                               //var file = new StringContent(, Encoding.UTF8, MediaTypeNames.Application.Zip);
                //                               var file = new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                //                               content.RemovePinataFile(file, "index.html");
                //                           },
                //                              metadata,
                //                              options);

                //                           if (response.IsSuccess)
                //                           {
                //                               //File uploaded to Pinata Cloud and can be accessed on IPFS!
                //                               var hash = response.IpfsHash; // QmR9HwzakHVr67HFzzgJHoRjwzTTt4wtD6KU4NFe2ArYuj
                //                           }

                //var pinataClient = new PinataClient("33e4469830a51af0171b");
                //PinFileResponse pinFileResponse = await pinataClient.PinFileToIPFSAsync(STARNETDNA.PublishedPath);

                //if (pinFileResponse != null && !string.IsNullOrEmpty(pinFileResponse.IpfsHash))
                //{
                //    STARNETDNA.PinataIPFSHash = pinFileResponse.IpfsHash;
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = true;
                //    STARNETDNA.STARNETHolonPublishedToPinata = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the T to Pinata.");
                //    STARNETDNA.STARNETHolonPublishedOnSTARNET = registerOnSTARNET && oappBinaryProviderType != ProviderType.None;
                //}
            }
            catch (Exception e)
            {
                CLIEngine.DisposeProgressBar(false);
                Console.WriteLine("");

                OASISErrorHandling.HandleWarning(ref result, $"An error occured publishing the {STARNETHolonUIName} to cloud storage. Reason: {e}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && binaryProviderType != ProviderType.None;
                STARNETDNA.PublishedToCloud = false;
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> UploadToOASISAsync(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            result.Result = new T1();
            result.Result.PublishedSTARNETHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARNETHolonResult = await UpdateAsync(avatarId, result.Result, providerType: binaryProviderType);

            if (saveLargeSTARNETHolonResult != null && !saveLargeSTARNETHolonResult.IsError && saveLargeSTARNETHolonResult.Result != null)
            {
                result.Result = saveLargeSTARNETHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARNETHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARNETHolonResult.Message}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
            }

            return result;
        }

        public OASISResult<T1> UploadToOASIS(Guid avatarId, T4 STARNETDNA, string publishedPath, bool registerOnSTARNET, bool uploadToCloud, ProviderType binaryProviderType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            result.Result = new T1();
            result.Result.PublishedSTARNETHolon = File.ReadAllBytes(publishedPath);

            //TODO: We could use HoloOASIS and other large file storage providers in future...
            OASISResult<T1> saveLargeSTARNETHolonResult = Update(avatarId, result.Result, providerType: binaryProviderType);

            if (saveLargeSTARNETHolonResult != null && !saveLargeSTARNETHolonResult.IsError && saveLargeSTARNETHolonResult.Result != null)
            {
                result.Result = saveLargeSTARNETHolonResult.Result;
                result.IsSaved = true;
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $" Error occured saving the published {STARNETHolonUIName} binary to STARNET using the {binaryProviderType} provider. Reason: {saveLargeSTARNETHolonResult.Message}");
                STARNETDNA.PublishedOnSTARNET = registerOnSTARNET && uploadToCloud;
                STARNETDNA.PublishedProviderType = Enum.GetName(typeof(ProviderType), ProviderType.None);
            }

            return result;
        }

        public virtual async Task<OASISResult<T1>> FininalizePublishAsync(Guid avatarId, T1 holon, bool edit, ProviderType providerType)
        {
            OASISResult<T1> result = new OASISResult<T1>();
            string errorMessage = "";

            //If its not the first version.
            if (holon.STARNETDNA.Version != "1.0.0" && !edit)
            {
                //If the ID has not been set then store the original id now.
                if (!holon.MetaData.ContainsKey(STARNETHolonIdName))
                    holon.MetaData[STARNETHolonIdName] = holon.Id;

                holon.MetaData["Version"] = holon.STARNETDNA.Version;
                holon.MetaData["VersionSequence"] = holon.STARNETDNA.VersionSequence;

                //Blank fields so it creates a new version.
                holon.Id = Guid.Empty;
                holon.ProviderUniqueStorageKey.Clear();
                holon.CreatedDate = DateTime.MinValue;
                holon.ModifiedDate = DateTime.MinValue;
                holon.CreatedByAvatarId = Guid.Empty;
                holon.ModifiedByAvatarId = Guid.Empty;
                holon.STARNETDNA.Downloads = 0;
                holon.STARNETDNA.Installs = 0;
            }

            OASISResult<T1> saveSTARNETHolonResult = await UpdateAsync(avatarId, holon, providerType: providerType);

            if (saveSTARNETHolonResult != null && !saveSTARNETHolonResult.IsError && saveSTARNETHolonResult.Result != null)
            {
                saveSTARNETHolonResult = await UpdateNumberOfVersionCountsAsync(avatarId, saveSTARNETHolonResult, errorMessage, providerType);
                result.IsSaved = true;
                result.Result = saveSTARNETHolonResult.Result; //TODO:Check if this is needed?

                CheckForVersionMismatches((T4)holon.STARNETDNA, ref result);

                if (result.IsWarning)
                    result.Message = $"{STARNETHolonUIName} successfully published but there were {result.WarningCount} warnings:\n\n {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                else
                    result.Message = $"{STARNETHolonUIName} Successfully Published";

                OnPublishStatusChanged?.Invoke(this, new STARNETHolonPublishStatusEventArgs() { STARNETDNA = holon.STARNETDNA, Status = STARNETHolonPublishStatus.Published });
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling SaveSTARNETHolonAsync on {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveSTARNETHolonResult.Message}");

            return result;
        }
    }
}
