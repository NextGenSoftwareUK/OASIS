using System.Linq;
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
            await Task.Run(() =>
            {
                var xDoc = new XmlDocument();
                xDoc.Load(_configurationPath);
                var xRoot = xDoc.DocumentElement;

                var keyNode = xRoot.Cast<XmlNode>().FirstOrDefault(node => node.Name == key);
                if (keyNode != null)
                {
                    keyNode.InnerText = value;
                    xDoc.Save(_configurationPath);
                    return;
                }

                var keyElement = xDoc.CreateElement(key);
                var valueText = xDoc.CreateTextNode(value);
                keyElement.AppendChild(valueText);
                xRoot.AppendChild(keyElement);
                xDoc.Save(_configurationPath);
            });
        }

        public async Task<string> GetKey(string key)
        {
            return await Task.Run(() =>
            {
                var xDoc = new XmlDocument();
                xDoc.Load(_configurationPath);
                var xRoot = xDoc.DocumentElement;
                var keyNode = xRoot.Cast<XmlNode>().FirstOrDefault(node => node.Name == key);
                return keyNode != null ? keyNode.InnerText : string.Empty;
            });
        }
    }
}