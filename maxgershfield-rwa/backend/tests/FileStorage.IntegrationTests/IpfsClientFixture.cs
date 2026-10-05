using BuildingBlocks.Extensions;
using Ipfs.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FileStorage.IntegrationTests;

public sealed class IpfsClientFixture
{
    public IOptions<IpfsOptions> AddFileOptions { get; private set; }
    public IpfsClient IpfsClient { get; private set; }
    public string Url { get; private set; }

    public IpfsClientFixture()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddIniFile("appsettings.ini")
            .Build();

        Url = configuration.GetRequiredString("IpfsSettings:Url");

        AddFileOptions = Options.Create(new IpfsOptions()
        {
            Pin = false
        });

        IpfsClient = new IpfsClient(Url);
    }
}
