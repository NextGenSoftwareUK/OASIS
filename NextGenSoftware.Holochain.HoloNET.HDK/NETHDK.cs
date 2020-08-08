using System;
using System.IO;

namespace NextGenSoftware.Holochain.NETHDK.Core
{
    public class NETHDK
    {
        public void Build(string classFolder)
        {
            if (!Directory.Exists(classFolder))
            {
                throw new ArgumentOutOfRangeException("classFolder", classFolder, "The folder is not valid, please double check and try again.");
            }

            string RustTemplateLib = "RustTemplates\\lib.rs";
            string RustTemplateCreate = "RustTemplates\\lib.rs";
            string RustTemplateRead = "RustTemplates\\lib.rs";
            string RustTemplateUpdate = "RustTemplates\\lib.rs";
            string RustTemplateDelete = "RustTemplates\\lib.rs";

            if (!File.Exists("RustTemplates\\lib.rs"))
            {
                throw new ArgumentOutOfRangeException("RustTemplateLib", classFolder, "The folder is not valid, please double check and try again.");
            }

            //Directory.GetFiles(classFolder)

            DirectoryInfo dirInfo = new DirectoryInfo(classFolder);
            FileInfo[] files = dirInfo.GetFiles();
            
            bool classLineReached = false;
            string varName;
            foreach (FileInfo file in files)
            {
                if (file != null)
                {
                    string buffer = file.OpenText().ReadLine();

                    if (classLineReached)
                    {
                        string[] parts = buffer.Split(' ');
                        varName = parts[2];

                        switch (parts[1].ToLower())
                        {
                            case "string":
                                break;

                            case "int":
                                break;

                            case "bool":
                                break;
                        }


                    }

                    if (buffer.Contains("class"))
                        classLineReached = true;

                    //string buffer = file.OpenText().ReadToEnd();
                }
            }
        }
    }
}
