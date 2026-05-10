using NoRecoil.Core;

namespace NoRecoil.Core.Tests;

public sealed class LastPathCacheTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), "norecoil-tests-" + Guid.NewGuid());

    [Fact]
    public void SaveThenLoad_ReturnsSavedPath()
    {
        var cache = new LastPathCache(_temporaryDirectory);
        var expectedPath = @"C:\\presets\\ak47.ini";

        cache.Save(expectedPath);

        var loaded = cache.Load();

        Assert.Equal(expectedPath, loaded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
