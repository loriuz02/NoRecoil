namespace NoRecoil.Core;

public sealed class LastPathCache
{
    private readonly string _cacheFilePath;

    public LastPathCache(string? cacheRootDirectory = null)
    {
        cacheRootDirectory ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NoRecoil");

        _cacheFilePath = Path.Combine(cacheRootDirectory, "last-preset-path.txt");
    }

    public string? Load()
    {
        if (!File.Exists(_cacheFilePath))
        {
            return null;
        }

        var path = File.ReadAllText(_cacheFilePath).Trim();
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var parent = Path.GetDirectoryName(_cacheFilePath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(_cacheFilePath, path);
    }
}
