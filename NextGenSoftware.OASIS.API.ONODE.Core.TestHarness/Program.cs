using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE ONODE Core TEST HARNESS V1.2");
            Console.WriteLine("");

            SampleManager sampleManager = new SampleManager(Guid.NewGuid());

            Console.WriteLine("Saving Sample Holon...");
            OASISResult<SampleHolon> saveSampleHolonResult = sampleManager.SaveSampleHolon("test wallet", "test avatar", Guid.NewGuid(), DateTime.Now, 77, 77777777777);

            if (!saveSampleHolonResult.IsError && saveSampleHolonResult.Result != null)
            {
                Console.WriteLine($"Sample Holon Saved. Id: {saveSampleHolonResult.Result.Id.ToString()}");

                Console.WriteLine("Loading Sample Holon...");
                OASISResult<SampleHolon> loadSampleHolonResult = sampleManager.LoadSampleHolon(saveSampleHolonResult.Result.Id);

                if (!loadSampleHolonResult.IsError && loadSampleHolonResult.Result != null)
                {
                    Console.WriteLine("SampleHolon Loaded.");
                    Console.WriteLine($"Id: {loadSampleHolonResult.Result.Id.ToString()}");
                    Console.WriteLine($"CustomProperty: {loadSampleHolonResult.Result.CustomProperty}");
                    Console.WriteLine($"CustomProperty2: {loadSampleHolonResult.Result.CustomProperty2}");
                    Console.WriteLine($"AvatarId: {loadSampleHolonResult.Result.AvatarId}");
                    Console.WriteLine($"CustomDate: {loadSampleHolonResult.Result.CustomDate.ToString()}");
                    Console.WriteLine($"CustomNumber: {loadSampleHolonResult.Result.CustomNumber.ToString()}");
                    Console.WriteLine($"CustomLongNumber: {loadSampleHolonResult.Result.CustomLongNumber.ToString()}");
                }
                else
                    Console.WriteLine($"Error Occured Loading Sample Holon. Reason: {loadSampleHolonResult.Message}");
            }
            else
                Console.WriteLine($"Error Occured Saving Sample Holon. Reason: {saveSampleHolonResult.Message}");


            /*
            NFTManager nftManager = new NFTManager();

            
            Console.WriteLine("Saving NFT Purchase Data...");
            OASISResult<IHolon> result = nftManager.PurchaseNFT("test wallet", "test avatar", Guid.NewGuid(), "tile data");

            if (!result.IsError && result.Result != null)
            { 
                Console.WriteLine($"NFT Purchase Data Saved. Id: {result.Result.Id.ToString()}");

                Console.WriteLine("Loading NFT Purchase Data...");
                OASISResult<IHolon> loadNFTPurchaseDataResult = nftManager.LoadNFTPurchaseData(result.Result.Id);

                if (!loadNFTPurchaseDataResult.IsError && loadNFTPurchaseDataResult.Result != null)
                {
                    Console.WriteLine("NFT Purchase Data Loaded.");
                    Console.WriteLine($"Id: {loadNFTPurchaseDataResult.Result.Id.ToString()}");
                    Console.WriteLine($"Wallet Address: {loadNFTPurchaseDataResult.Result.MetaData["WalletAddress"]}");
                    Console.WriteLine($"AvatarUsername: {loadNFTPurchaseDataResult.Result.MetaData["AvatarUsername"]}");
                    Console.WriteLine($"AvatarId: {loadNFTPurchaseDataResult.Result.MetaData["AvatarId"]}");
                    Console.WriteLine($"JsonSelectedTiles: {loadNFTPurchaseDataResult.Result.MetaData["JsonSelectedTiles"]}");
                }
                else
                    Console.WriteLine($"Error Occured Loading NFT Purchase Data. Reason: {loadNFTPurchaseDataResult.Message}");
            }
            else
                Console.WriteLine($"Error Occured Saving NFT Purchase Data. Reason: {result.Message}");
            


            Console.WriteLine("Saving NFT Purchase Data2...");
            OASISResult<PurchaseNFTHolon> purchaseHolonResult = nftManager.PurchaseNFT2("test wallet", "test avatar", Guid.NewGuid(), "tile data");

            if (!purchaseHolonResult.IsError && purchaseHolonResult.Result != null)
            {
                Console.WriteLine($"NFT Purchase Data Saved. Id: {purchaseHolonResult.Result.Id.ToString()}");

                Console.WriteLine("Loading NFT Purchase Data2...");
                OASISResult<PurchaseNFTHolon> loadNFTPurchaseDataResult = nftManager.LoadNFTPurchaseData2(purchaseHolonResult.Result.Id);

                if (!loadNFTPurchaseDataResult.IsError && loadNFTPurchaseDataResult.Result != null)
                {
                    Console.WriteLine("NFT Purchase Data Loaded.");
                    Console.WriteLine($"Id: {loadNFTPurchaseDataResult.Result.Id.ToString()}");
                    Console.WriteLine($"Wallet Address: {loadNFTPurchaseDataResult.Result.WalletAddress}");
                    Console.WriteLine($"AvatarUsername: {loadNFTPurchaseDataResult.Result.AvatarUsername}");
                    Console.WriteLine($"AvatarId: {loadNFTPurchaseDataResult.Result.AvatarId}");
                    Console.WriteLine($"JsonSelectedTiles: {loadNFTPurchaseDataResult.Result.JsonSelectedTiles}");
                }
                else
                    Console.WriteLine($"Error Occured Loading NFT Purchase Data. Reason: {loadNFTPurchaseDataResult.Message}");

                Console.WriteLine("Loading NFT Purchase Data3...");
                OASISResult<PurchaseNFTHolon> loadNFTPurchaseDataResult3 = nftManager.LoadNFTPurchaseData3(purchaseHolonResult.Result.Id);

                if (!loadNFTPurchaseDataResult3.IsError && loadNFTPurchaseDataResult3.Result != null)
                {
                    Console.WriteLine("NFT Purchase Data Loaded.");
                    Console.WriteLine($"Id: {loadNFTPurchaseDataResult3.Result.Id.ToString()}");
                    Console.WriteLine($"Wallet Address: {loadNFTPurchaseDataResult3.Result.WalletAddress}");
                    Console.WriteLine($"AvatarUsername: {loadNFTPurchaseDataResult3.Result.AvatarUsername}");
                    Console.WriteLine($"AvatarId: {loadNFTPurchaseDataResult3.Result.AvatarId}");
                    Console.WriteLine($"JsonSelectedTiles: {loadNFTPurchaseDataResult3.Result.JsonSelectedTiles}");
                }
                else
                    Console.WriteLine($"Error Occured Loading NFT Purchase Data. Reason: {loadNFTPurchaseDataResult3.Message}");

            }
            else
                Console.WriteLine($"Error Occured Saving NFT Purchase Data. Reason: {purchaseHolonResult.Message}");
            */
        }
    }
}
