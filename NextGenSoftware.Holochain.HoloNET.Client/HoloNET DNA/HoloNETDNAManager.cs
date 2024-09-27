using System;
using System.IO;
using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public static class HoloNETDNAManager
    {
        //public static string HoloNETDNAPath = "HoloNET_DNA.json";
        //public static string HoloNETDNAPath = "HoloNET_DNA.json";
        public static string HoloNETDNAPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NextGenSoftware\\HoloNET\\HoloNETDNA.json");
        public static IHoloNETDNA HoloNETDNA { get; set; } = new HoloNETDNA();

        public static IHoloNETDNA LoadDNA()
        {
            return LoadDNA(HoloNETDNAPath);
        }

        public static IHoloNETDNA LoadDNA(string holoNETDNAPath)
        {
            try
            {
                if (string.IsNullOrEmpty(holoNETDNAPath))
                    throw new ArgumentNullException("holoNETDNAPath", "holoNETDNAPath cannot be null."); //TODO: Need to come back to this since this exception will always be caught below! ;-)

                HoloNETDNAPath = holoNETDNAPath;

                using (StreamReader r = new StreamReader(holoNETDNAPath))
                {
                    string json = r.ReadToEnd();
                    HoloNETDNA = JsonConvert.DeserializeObject<IHoloNETDNA>(json);
                    IsLoaded = true;
                    return HoloNETDNA;
                }
            }
            catch (Exception ex) 
            {
                return null; 
            }
        }

        public static bool IsLoaded { get; private set; }

        public static bool SaveDNA()
        {
            return SaveDNA(HoloNETDNAPath, HoloNETDNA);
        }

        public static bool SaveDNA(string holoNETDNAPath, IHoloNETDNA holoNETDNA)
        {
            try
            {
                if (string.IsNullOrEmpty(holoNETDNAPath))
                    throw new ArgumentNullException("holoNETDNA", "holoNETDNA cannot be null."); //TODO: Need to come back to this since this exception will always be caught below! ;-)

                if (holoNETDNA == null)
                    throw new ArgumentNullException("holoNETDNA", "holoNETDNA cannot be null."); //TODO: Need to come back to this since this exception will always be caught below! ;-)

                FileInfo fileInfo = new FileInfo(holoNETDNAPath);

                if (!Directory.Exists(fileInfo.DirectoryName))
                    Directory.CreateDirectory(fileInfo.DirectoryName);

                HoloNETDNA = holoNETDNA;
                HoloNETDNAPath = holoNETDNAPath;

                string json = JsonConvert.SerializeObject(holoNETDNA);
                StreamWriter writer = new StreamWriter(holoNETDNAPath);
                writer.Write(json);
                writer.Close();

                return true;
            }
            catch (Exception ex) 
            {
                return false;
            }
        }
    }
}