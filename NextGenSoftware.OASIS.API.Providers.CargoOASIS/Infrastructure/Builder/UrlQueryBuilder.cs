using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Builder
{
    public class UrlQueryBuilder
    {
        private readonly StringBuilder _builder;

        public UrlQueryBuilder()
        {
            _builder = new StringBuilder("?");
        }

        public void AppendParameter(string name, string value)
        {
            if(!string.IsNullOrEmpty(value))
                _builder.AppendFormat("&{0}={1}", name, value);
        }

        public string GetQuery()
        {
            return _builder.ToString();
        }
    }
}