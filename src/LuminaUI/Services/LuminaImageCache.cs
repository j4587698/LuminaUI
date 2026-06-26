using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public sealed class LuminaImageCache : ILuminaImageCache
{
    private sealed record MemoryEntry(byte[] Bytes, DateTimeOffset ExpiresAt);

    private static readonly LuminaAsyncLockStripes DiskLocks = new(64);

    private readonly ConcurrentDictionary<string, MemoryEntry> _memoryCache = new();

    public async Task<byte[]?> GetAsync(string key, LuminaImageLoadOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (options.CacheMode is LuminaImageCacheMode.Memory or LuminaImageCacheMode.MemoryAndDisk
            && _memoryCache.TryGetValue(key, out MemoryEntry? entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return entry.Bytes;
        }
        if (options.CacheMode is LuminaImageCacheMode.Disk or LuminaImageCacheMode.MemoryAndDisk
            && TryGetCacheFilePath(key, options.CacheDirectory, out string cacheFile))
        {
            SemaphoreSlim diskLock = DiskLocks.GetLock(cacheFile);
            await diskLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            try
            {
                byte[]? cachedBytes = await TryReadFreshCacheAsync(cacheFile, options.CacheDuration, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                if (cachedBytes != null)
                {
                    if (options.CacheMode == LuminaImageCacheMode.MemoryAndDisk)
                    {
                        _memoryCache[key] = new MemoryEntry(cachedBytes, DateTimeOffset.UtcNow.Add(options.CacheDuration));
                    }
                    return cachedBytes;
                }
            }
            finally
            {
                diskLock.Release();
            }
        }
        return null;
    }

    public async Task SetAsync(string key, byte[] bytes, LuminaImageLoadOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (options.CacheMode is LuminaImageCacheMode.Memory or LuminaImageCacheMode.MemoryAndDisk)
        {
            _memoryCache[key] = new MemoryEntry(bytes, DateTimeOffset.UtcNow.Add(options.CacheDuration));
        }
        if (options.CacheMode is not (LuminaImageCacheMode.Disk or LuminaImageCacheMode.MemoryAndDisk)
            || !TryGetCacheFilePath(key, options.CacheDirectory, out string cacheFile))
        {
            return;
        }
        SemaphoreSlim diskLock = DiskLocks.GetLock(cacheFile);
        await diskLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            await WriteCacheFileAtomicAsync(cacheFile, bytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Writing to the disk cache is best-effort: an IO failure (disk full, permissions, etc.)
            // must not break image loading, but it should not be silently swallowed either.
            System.Diagnostics.Debug.WriteLine($"LuminaImageCache: failed to write cache file '{cacheFile}': {ex}");
        }
        finally
        {
            diskLock.Release();
        }
    }

    public void Clear()
    {
        _memoryCache.Clear();
    }

    private static async Task<byte[]?> TryReadFreshCacheAsync(string cacheFile, TimeSpan duration, CancellationToken cancellationToken)
    {
        if (!File.Exists(cacheFile) || duration <= TimeSpan.Zero)
        {
            return null;
        }
        DateTime lastWrite = File.GetLastWriteTimeUtc(cacheFile);
        if (DateTime.UtcNow - lastWrite > duration)
        {
            return null;
        }
        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(cacheFile, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return (bytes.Length != 0) ? bytes : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static async Task WriteCacheFileAtomicAsync(string cacheFile, byte[] bytes, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(cacheFile);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }
        Directory.CreateDirectory(directory);
        string tempFile = Path.Combine(directory, $"{Path.GetFileName(cacheFile)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (FileStream stream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
            File.Move(tempFile, cacheFile, overwrite: true);
        }
        finally
        {
            TryDeleteFile(tempFile);
        }
    }

    private static void TryDeleteFile(string file)
    {
        try
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch
        {
        }
    }

    private static bool TryGetCacheFilePath(string key, string? cacheDirectory, out string cacheFile)
    {
        cacheFile = string.Empty;
        try
        {
            string root = string.IsNullOrWhiteSpace(cacheDirectory) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LuminaUI", "ImageCache") : cacheDirectory;
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Path.GetTempPath(), "LuminaUI", "ImageCache");
            }
            Directory.CreateDirectory(root);
            cacheFile = Path.Combine(root, Hash(key) + ".img");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Hash(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
