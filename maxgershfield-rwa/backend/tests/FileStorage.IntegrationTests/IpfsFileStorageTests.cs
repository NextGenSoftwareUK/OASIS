using System.Runtime.CompilerServices;

namespace FileStorage.IntegrationTests;

public sealed class IpfsFileStorageTests(IpfsClientFixture fixture) : IClassFixture<IpfsClientFixture>
{
    [Fact]
    public async Task IpfsFileStorage_CreateAsync_MustUploadFileToIpfsNode_Scenario()
    {
        // Arrange.
        IpfsFileStorage fileStorage = new(fixture.IpfsClient, fixture.AddFileOptions);

        string fileName = "image.png";
        using FileStream fileStream = File.OpenRead(fileName);

        // Act.
        string cid = await fileStorage.CreateAsync(fileStream, fileName, CancellationToken.None);

        // Assert.
        Assert.NotEmpty(cid);
    }

    [Fact]
    public async Task IpfsFileStorage_GetAsync_MustUploadAndRetrieveFileByCid_Scenario()
    {
        // Arrange.
        IpfsFileStorage fileStorage = new(fixture.IpfsClient, fixture.AddFileOptions);

        string fileName = "image.png";
        string downloadedFileName = "downloaded_image.png";

        using FileStream fileStream = File.OpenRead(fileName);

        // Act.
        string cid = await fileStorage.CreateAsync(fileStream, fileName, CancellationToken.None);

        IAsyncEnumerable<ReadOnlyMemory<byte>> stream = fileStorage.GetAsync(cid, token: CancellationToken.None);

        // Save the downloaded file.
        await using (FileStream downloadedFileStream = File.Create(downloadedFileName))
        {
            await foreach (ReadOnlyMemory<byte> chunk in stream)
            {
                await downloadedFileStream.WriteAsync(chunk);
            }
        }

        // Assert.
        Assert.True(CompareFiles(fileName, downloadedFileName), "The files are not identical.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareFiles(string file1Path, string file2Path)
        {
            using FileStream fs1 = File.OpenRead(file1Path);
            using FileStream fs2 = File.OpenRead(file2Path);

            // If the file sizes are different, they are not the same
            if (fs1.Length != fs2.Length)
                return false;

            // Compare bytes of both files
            int byte1;
            int byte2;
            while ((byte1 = fs1.ReadByte()) != -1 && (byte2 = fs2.ReadByte()) != -1)
            {
                if (byte1 != byte2)
                    return false;
            }

            return true;
        }
    }
}