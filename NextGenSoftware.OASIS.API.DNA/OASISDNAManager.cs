using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.DNA
{
    public static class OASISDNAManager
    {
        public static string OASISDNAPath = "OASIS_DNA.json";
        public static OASISDNA OASISDNA { get; set; }

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

                else if (File.Exists(OASISDNAPath))
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