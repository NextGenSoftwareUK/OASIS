﻿using System;
using System.IO;
using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Config
{
    public static class OASISConfigManager 
    {
        public static string OASISDNAFileName { get; set; } = "appsettings.json";
        public static OASISDNA OASISDNA;

        public static void LoadOASISDNA(string OASISDNAFileName)
        {
            OASISConfigManager.OASISDNAFileName = OASISDNAFileName;

            if (File.Exists(OASISDNAFileName))
            {
                using (StreamReader r = new StreamReader(OASISDNAFileName))
                {
                    string json = r.ReadToEnd();
                    OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                }
            }
        }

        public static IOASISStorage GetAndActivateProvider()
        {
            if (ProviderManager.CurrentStorageProvider == null)
            {
                if (OASISDNA == null)
                    LoadOASISDNA(OASISDNAFileName);

                ProviderManager.DefaultProviderTypes = OASISDNA.OASIS.StorageProviders.DefaultProviders.Split(",");

                //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc...
                return GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
            }
            else
                return ProviderManager.CurrentStorageProvider;
        }

        public static IOASISStorage GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            if (OASISDNA == null)
                LoadOASISDNA(OASISDNAFileName);

            //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
            if (providerType != ProviderManager.CurrentStorageProviderType.Value)
            {
                RegisterProvider(providerType);
                ProviderManager.SetAndActivateCurrentStorageProvider(providerType, setGlobally);

            }

            if (setGlobally && ProviderManager.CurrentStorageProvider != ProviderManager.DefaultGlobalStorageProvider)
                ProviderManager.DefaultGlobalStorageProvider = ProviderManager.CurrentStorageProvider;

            ProviderManager.OverrideProviderType = true;
            return ProviderManager.CurrentStorageProvider; 
        }

        public static IOASISStorage RegisterProvider(ProviderType providerType)
        {
            IOASISStorage registeredProvider = null;

            if (OASISDNA == null)
                LoadOASISDNA(OASISDNAFileName);

            if (!ProviderManager.IsProviderRegistered(providerType))
            {
                switch (providerType)
                {
                    case ProviderType.HoloOASIS:
                        {
                            HoloOASIS holoOASIS = new HoloOASIS(OASISDNA.OASIS.StorageProviders.HoloOASIS.ConnectionString, HolochainVersion.Redux); //TODO: Move hc version to config.
                            holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                            holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(holoOASIS);
                            registeredProvider = holoOASIS;
                        }
                        break;

                    case ProviderType.SQLLiteDBOASIS:
                        {
                            SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(OASISDNA.OASIS.StorageProviders.SQLLiteDBOASIS.ConnectionString);
                            SQLLiteDBOASIS.StorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(SQLLiteDBOASIS);
                            registeredProvider = SQLLiteDBOASIS;
                        }
                        break;

                    case ProviderType.MongoDBOASIS:
                        {
                            MongoDBOASIS mongoOASIS = new MongoDBOASIS(OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.MongoDBOASIS.DBName);
                            mongoOASIS.StorageProviderError += MongoOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(mongoOASIS);
                            registeredProvider = mongoOASIS;
                        }
                        break;

                    case ProviderType.EOSOASIS:
                        {
                            EOSIOOASIS EOSIOOASIS = new EOSIOOASIS(OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString);
                            EOSIOOASIS.StorageProviderError += EOSIOOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(EOSIOOASIS); //TODO: Need to pass connection string in.
                            registeredProvider = EOSIOOASIS;
                        }
                        break;

                    case ProviderType.Neo4jOASIS:
                        {
                            Neo4jOASIS Neo4jOASIS = new Neo4jOASIS(OASISDNA.OASIS.StorageProviders.Neo4jOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Username, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Password);
                            Neo4jOASIS.StorageProviderError += Neo4jOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(Neo4jOASIS); //TODO: Need to pass connection string in.
                            registeredProvider = Neo4jOASIS;
                        }
                        break;

                    case ProviderType.IPFSOASIS:
                        {
                            IPFSOASIS IPFSOASIS = new IPFSOASIS(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
                            IPFSOASIS.StorageProviderError += IPFSOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(IPFSOASIS); //TODO: Need to pass connection string in.
                            registeredProvider = IPFSOASIS;
                        }
                        break;
                }
            }
            else
                registeredProvider = (IOASISStorage)ProviderManager.GetProvider(providerType);

            return registeredProvider;
        }


        private static void IPFSOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Neo4jOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void SQLLiteDBOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: MongoOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void EOSIOOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            // throw new Exception(string.Concat("ERROR: EOSIOOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void MongoOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: MongoOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void HoloOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void HoloOASIS_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_OnHoloOASISError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNET.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNET.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }
    }
}
