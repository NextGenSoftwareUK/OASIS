using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    public static class STARDNAManager
    {
        public static string STARDNAPath = Path.Combine("DNA", "STARDNA.json");
        public static STARDNA STARDNA { get; set; }

        public static OASISResult<STARDNA> LoadDNA()
        {
            return LoadDNA(STARDNAPath);
        }

        public static async Task<OASISResult<STARDNA>> LoadDNAAsync()
        {
            return await LoadDNAAsync(STARDNAPath);
        }

        public static OASISResult<STARDNA> LoadDNA(string STARDNAPath)
        {
            OASISResult<STARDNA> result = new OASISResult<STARDNA>();
            string errorMessage = "Error occured in STARDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");

                else if (!File.Exists(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STARDNA.json file.");
                
                else
                {
                    STARDNAManager.STARDNAPath = STARDNAPath;

                    using (StreamReader r = new StreamReader(STARDNAPath))
                    {
                        string json = r.ReadToEnd();
                        STARDNA = JsonConvert.DeserializeObject<STARDNA>(json);
                        result.Result = STARDNA;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<STARDNA>> LoadDNAAsync(string STARDNAPath)
        {
            OASISResult<STARDNA> result = new OASISResult<STARDNA>();
            string errorMessage = "Error occured in STARDNAManager.LoadDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");

                else if (!File.Exists(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STARDNA.json file.");

                else
                {
                    STARDNAManager.STARDNAPath = STARDNAPath;

                    using (StreamReader r = new StreamReader(STARDNAPath))
                    {
                        string json = await r.ReadToEndAsync();
                        STARDNA = JsonConvert.DeserializeObject<STARDNA>(json);
                        result.Result = STARDNA;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<bool> SaveDNA()
        {
            return SaveDNA(STARDNAPath, STARDNA);
        }

        public static OASISResult<bool> SaveDNA(string STARDNAPath, STARDNA STARDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");

                else if (!File.Exists(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STARDNA.json file.");

                else
                {
                    if (STARDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.SaveDNA. Reason: STARDNA cannot be null.");

                    STARDNAManager.STARDNA = STARDNA;
                    STARDNAManager.STARDNAPath = STARDNAPath;

                    if (!Directory.Exists(Path.GetDirectoryName(STARDNAPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(STARDNAPath));

                    string json = JsonConvert.SerializeObject(STARDNA);
                    StreamWriter writer = new StreamWriter(STARDNAPath);
                    writer.Write(json);
                    writer.Close();
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync()
        {
            return await SaveDNAAsync(STARDNAPath, STARDNA);
        }

        public static async Task<OASISResult<bool>> SaveDNAAsync(string STARDNAPath, STARDNA STARDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in STARDNAManager.SaveDNA. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNAPath cannot be null.");

                else if (!File.Exists(STARDNAPath))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The STARDNAPath ({STARDNAPath}) is not valid. Please make sure the STARDNAPath is valid and that it points to the STARDNA.json file.");

                else
                {
                    if (STARDNA == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}STARDNA cannot be null.");

                    STARDNAManager.STARDNA = STARDNA;
                    STARDNAManager.STARDNAPath = STARDNAPath;

                    if (!Directory.Exists(Path.GetDirectoryName(STARDNAPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(STARDNAPath));

                    string json = JsonConvert.SerializeObject(STARDNA);
                    StreamWriter writer = new StreamWriter(STARDNAPath);
                    await writer.WriteAsync(json);
                    writer.Close();
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STARDNAManager.LoadDNA. Reason: {ex.Message}");
            }

            return result;
        }
    }
}