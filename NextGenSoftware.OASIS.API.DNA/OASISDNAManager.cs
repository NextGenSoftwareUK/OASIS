using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.DNA
{
    public static class OASISDNAManager
    {
        private static string SYSTEMOASISDNAPath = "OASIS_DNA_SYSTEM.json";
        public static string OASISDNAPath = "OASIS_DNA.json";
        public static OASISDNA OASISDNA { get; set; }
        //public static OASISDNA SYSTEMOASISDNA { get; set; }

        // To mitigate the risk of exposing sensitive keys in open-source code, consider the following approaches:

        // 1. Use environment variables to store the key securely and retrieve it at runtime.
        // 2. Use a secure secrets management service (e.g., Azure Key Vault, AWS Secrets Manager).
        // 3. Encrypt the key using a hardware security module (HSM) or a similar secure mechanism.
        // 4. Avoid hardcoding sensitive keys in the source code entirely.
        // 5. Implement runtime obfuscation techniques to make debugging harder, though this is not foolproof.

        // Example: Using environment variables to retrieve the key securely.
        //public static OASISResult<OASISDNA> GetSystemOASISDNA()
        //{
        //    OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
        //    string systemKey = Environment.GetEnvironmentVariable("SYSTEM_OASIS_DNA_KEY");

        //    if (string.IsNullOrEmpty(systemKey))
        //    {
        //        OASISErrorHandling.HandleError(ref result, "System OASIS DNA key is not set in environment variables.");
        //        return result;
        //    }

        //    try
        //    {
        //        using (StreamReader r = new StreamReader(SYSTEMOASISDNAPath))
        //        {
        //            string encryptedData = r.ReadToEnd();
        //            result.Result = JsonConvert.DeserializeObject<OASISDNA>(FileEncryption.Decrypt(encryptedData, systemKey));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occurred while decrypting SYSTEMOASISDNA: {ex.Message}");
        //    }

        //    return result;
        //}



        //TODO: HOW DO WE STOP PEOPLE PUTTING A BREAKPOINT HERE AND GETTING THE KEY OR DECRYPTED FILE WHEN IT'S OPEN SOURCE?!?!?!?!
        public static OASISResult<OASISDNA> GetSystemOASISDNA()
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            result.Result = JsonConvert.DeserializeObject<OASISDNA>(FileEncryption.DecryptFile(SYSTEMOASISDNAPath, DNALoader.DNALoader.GetSystemOASISDNAKey(), DNALoader.DNALoader.GetSystemOASISDNAIV()));

            //using (StreamReader r = new StreamReader(SYSTEMOASISDNAPath))
            //    result.Result = JsonConvert.DeserializeObject<OASISDNA>(FileEncryption.Decrypt(r.ReadToEnd(), DNALoader.DNALoader.GetSystemOASISDNAKey()));

            return result;
        }

        //public static OASISResult<OASISDNA> EncryptSystemOASISDNA()
        //{
        //    OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();

        //    using (StreamReader r = new StreamReader(SYSTEMOASISDNAPath))
        //    {
        //        string json = r.ReadToEnd();

        //        if (!string.IsNullOrEmpty(privateKey))
        //        {
        //            //Decrypt the JSON first before deserializing:
        //            //json = CryptoHelper.DecryptStringFromBytes_Aes(, privateKey);

        //            // Generate a random AES key and IV
        //            using (Aes aes = Aes.Create())
        //            {
        //                byte[] key = aes.Key;
        //                byte[] iv = aes.IV;

        //                // Call the encryption method
        //                FileEncryption.EncryptFile(OASISDNAPath, SYSTEMOASISDNAPath, key, iv);

        //                Console.WriteLine("File encrypted successfully!");
        //                Console.WriteLine($"Key: {Convert.ToBase64String(key)}");
        //                Console.WriteLine($"IV: {Convert.ToBase64String(iv)}");
        //            }

        //        }

        //        OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
        //        result.Result = OASISDNA;
        //    }

        //    return result;
        //}

        public static OASISResult<OASISDNA> LoadDNA()
        {
            return LoadDNA(OASISDNAPath);
        }

        public static async Task<OASISResult<OASISDNA>> LoadDNAAsync()
        {
            return await LoadDNAAsync(OASISDNAPath);
        }

        public static OASISResult<OASISDNA> LoadDNA(string OASISDNAPath)
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            string errorMessage = "Error occured in OASISDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");
                
                else
                {
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    using (StreamReader r = new StreamReader(OASISDNAPath))
                    {
                        string json = r.ReadToEnd();
                        OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                        result.Result = OASISDNA;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<OASISDNA>> LoadDNAAsync(string OASISDNAPath)
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            string errorMessage = "Error occured in OASISDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");

                else
                {
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    using (StreamReader r = new StreamReader(OASISDNAPath))
                    {
                        string json = await r.ReadToEndAsync();
                        OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                        result.Result = OASISDNA;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<bool> SaveDNA()
        {
            return SaveDNA(OASISDNAPath, OASISDNA);
        }

        public static OASISResult<bool> SaveDNA(string OASISDNAPath, OASISDNA OASISDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OASISDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");

                else
                {
                    if (OASISDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.SaveDNA. Reason: OASISDNA cannot be null.");

                    OASISDNAManager.OASISDNA = OASISDNA;
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    string json = JsonConvert.SerializeObject(OASISDNA);
                    StreamWriter writer = new StreamWriter(OASISDNAPath);
                    writer.Write(json);
                    writer.Close();
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync()
        {
            return await SaveDNAAsync(OASISDNAPath, OASISDNA);
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync(string OASISDNAPath, OASISDNA OASISDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in OASISDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNAPath cannot be null.");

                else if (!File.Exists(OASISDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The OASISDNAPath ({OASISDNAPath}) is not valid. Please make sure the OASISDNAPath is valid and that it points to the OASISDNA.json file.");

                else
                {
                    if (OASISDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}OASISDNA cannot be null.");

                    OASISDNAManager.OASISDNA = OASISDNA;
                    OASISDNAManager.OASISDNAPath = OASISDNAPath;

                    string json = JsonConvert.SerializeObject(OASISDNA);
                    StreamWriter writer = new StreamWriter(OASISDNAPath);
                    await writer.WriteAsync(json);
                    writer.Close();
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OASISDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }
    }
}