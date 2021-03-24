using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    // Might have been ProviderManagerConfig before global replaceall from Profile to Avatar... Check later... :)
    public class ProviderManagerConfig
    {
        private FieldToProviderMappingsConfig _fieldToProviderMappings = null;

        public FieldToProviderMappingsConfig FieldToProviderMappings
        {
            get
            {
                if (_fieldToProviderMappings == null)
                {
                    _fieldToProviderMappings = new FieldToProviderMappingsConfig();
                }

                return _fieldToProviderMappings;
            }
        }

        //TODO: Not sure want to split Avatar across multiple providers? How fast would it be? Need to check GraphQL (Apollo)...
        public class FieldToProviderMappingsConfig
        {
            public FieldToProviderMappingsConfig()
            {
                Name = new List<FieldToProviderMappingAccess>();
                DOB = new List<FieldToProviderMappingAccess>();
                Email = new List<FieldToProviderMappingAccess>();
                UserName = new List<FieldToProviderMappingAccess>();
                Karma = new List<FieldToProviderMappingAccess>();
            }

            public List<FieldToProviderMappingAccess> Name { get; set; }
            public List<FieldToProviderMappingAccess> DOB { get; set; }
            public List<FieldToProviderMappingAccess> Email { get; set; }

            public List<FieldToProviderMappingAccess> UserName { get; set; }

            public List<FieldToProviderMappingAccess> Karma { get; set; }
        }

        public class FieldToProviderMappingAccess
        {
            public ProviderType Provider { get; set; }
            public ProviderAccess Access { get; set; }
        }

        public enum ProviderAccess
        {
            ReadOnly,
            ReadWrite,
            Store
        }

    }
}