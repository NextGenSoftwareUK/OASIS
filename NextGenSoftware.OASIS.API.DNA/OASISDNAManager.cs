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

        private static OASISResult<OASISDNA> GetSystemOASISDNA()
        {
            OASISResult<OASISDNA> result = new OASISResult<OASISDNA>();
            result.Result = JsonConvert.DeserializeObject<OASISDNA>(FileEncryption.DecryptFile(SYSTEMOASISDNAPath, DNALoader.DNALoader.GetSystemOASISDNAKey(), DNALoader.DNALoader.GetSystemOASISDNAIV()));
            return result;
        }

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

                        //OASISResult<OASISDNA> OASISDNAResult = GetSystemOASISDNA();

                        //if (OASISDNAResult != null && OASISDNAResult.Result != null && !OASISDNAResult.IsError)
                        //{
                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.EmailFrom))
                        //        OASISDNA.OASIS.Email.EmailFrom = OASISDNAResult.Result.OASIS.Email.EmailFrom;

                        //    if (OASISDNA.OASIS.Email.SmtpPort == 0)
                        //        OASISDNA.OASIS.Email.SmtpPort = OASISDNAResult.Result.OASIS.Email.SmtpPort;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpPass))
                        //        OASISDNA.OASIS.Email.SmtpPass = OASISDNAResult.Result.OASIS.Email.SmtpPass;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpHost))
                        //        OASISDNA.OASIS.Email.SmtpHost = OASISDNAResult.Result.OASIS.Email.SmtpHost;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpUser))
                        //        OASISDNA.OASIS.Email.SmtpHost = OASISDNAResult.Result.OASIS.Email.SmtpUser;

                        //    if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.OASISWebSiteURL))
                        //        OASISDNA.OASIS.Email.OASISWebSiteURL = OASISDNAResult.Result.OASIS.Email.OASISWebSiteURL;

                        //    //TODO: Finish implementing for the rest of the properties! :)

                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.Provider))
                        //    //    OASISDNA.OASIS.Provider = OASISDNAResult.Result.OASIS.Provider;
                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.DefaultStorageProvider))
                        //    //    OASISDNA.OASIS.DefaultStorageProvider = OASISDNAResult.Result.OASIS.DefaultStorageProvider;
                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.FallbackStorageProvider))
                        //    //    OASISDNA.OASIS.FallbackStorageProvider = OASISDNAResult.Result.OASIS.FallbackStorageProvider;
                        //    //if (string.IsNullOrEmpty(OASISDNA.OASIS.AvailableProviders))
                        //    //    OASISDNA.OASIS.AvailableProviders = OASISDNAResult.Result.OASIS.AvailableProviders;
                        //}

                        //if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.EmailFrom))
                        //    OASISDNA.OASIS.Email.EmailFrom = GetSystemOASISDNA().Result.OASIS.Email.EmailFrom;

                        //if (OASISDNA.OASIS.Email.SmtpPort == 0)
                        //    OASISDNA.OASIS.Email.SmtpPort = GetSystemOASISDNA().Result.OASIS.Email.SmtpPort;

                        //if (string.IsNullOrEmpty(OASISDNA.OASIS.Email.SmtpPass))
                        //    OASISDNA.OASIS.Email.SmtpPass = GetSystemOASISDNA().Result.OASIS.Email.SmtpPass;

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