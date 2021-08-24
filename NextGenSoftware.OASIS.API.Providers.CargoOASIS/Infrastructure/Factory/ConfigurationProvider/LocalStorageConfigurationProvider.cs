using System.Threading.Tasks;
using System.Xml;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Factory.ConfigurationProvider
{
    public class LocalStorageConfigurationProvider : IConfigurationProvider
    {
        private readonly string _configurationPath;

        public LocalStorageConfigurationProvider()
        {
            _configurationPath = "config.xml";
        }
        
        public async Task SetKey(string key, string value)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(_configurationPath);
            var xRoot = xDoc.DocumentElement;
            var keyElement = xDoc.CreateElement(key);
            
            var valueText = xDoc.CreateTextNode("Facebook");
            
            
            companyElem.AppendChild(companyText);
            ageElem.AppendChild(ageText);
            keyElement.AppendChild(companyElem);
            keyElement.AppendChild(ageElem);
            xRoot.AppendChild(keyElement);
            xDoc.Save(_configurationPath);
        }

        public async Task<string> GetKey(string key)
        {
            throw new System.NotImplementedException();
        }
    }
}