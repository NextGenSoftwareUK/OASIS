namespace ScGen.Lib.Shared.Extensions;

public static class DirectoryExtensions
{
    public static void DeleteDirectorySafe(this string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    public static void LogDirectoryContents(string directory, ILogger logger)
    {
        if (!Directory.Exists(directory))
            return;
        foreach (string dir in Directory.GetDirectories(directory))
            logger.LogError("  DIR: {Name}", Path.GetFileName(dir));

        foreach (string file in Directory.GetFiles(directory))
            logger.LogError("  FILE: {Name}", Path.GetFileName(file));
    }

    public static void CopyDirectory(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, targetSubDir);
        }
    }
}