using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public sealed class LuminaImageCache : ILuminaImageCache
{
    private const long Megabyte = 1024L * 1024L;

    private sealed record MemoryEntry(string Key, byte[] Bytes, DateTimeOffset ExpiresAt);

    private sealed record DiskEntry(string Path, long Length, DateTime LastWriteTimeUtc);

    private static readonly LuminaAsyncLockStripes DiskLocks = new(64);

    private readonly object _memoryGate = new();

    private readonly object _diskDirectoriesGate = new();

    private readonly Dictionary<string, LinkedListNode<MemoryEntry>> _memoryMap = new(StringComparer.Ordinal);

    private readonly LinkedList<MemoryEntry> _memoryLru = new();

    private long _memorySizeBytes;

    private long _memoryCapacityBytes;

    private long _diskCapacityBytes;

    private long _estimatedDiskSizeBytes = -1;

    private int _isDiskCacheEnabled;

    private readonly SemaphoreSlim _diskMaintenanceLock = new(1, 1);

    private int _diskMaintenanceScheduled;

    private long _nextDiskMaintenanceUtcTicks;

    private readonly string _defaultDiskCacheDirectory;

    private readonly HashSet<string> _diskDirectories = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    public LuminaImageCache()
        : this(GetDefaultMemoryCapacityBytes())
    {
    }

    public LuminaImageCache(long memoryCapacityBytes)
    {
        _memoryCapacityBytes = Math.Max(0, memoryCapacityBytes);
        _diskCapacityBytes = GetDefaultDiskCapacityBytes();
        _isDiskCacheEnabled = OperatingSystem.IsBrowser() ? 0 : 1;
        _defaultDiskCacheDirectory = ResolveDefaultDiskCacheDirectory();
        if (!string.IsNullOrWhiteSpace(_defaultDiskCacheDirectory))
        {
            _defaultDiskCacheDirectory = Path.GetFullPath(_defaultDiskCacheDirectory);
            _diskDirectories.Add(_defaultDiskCacheDirectory);
        }
    }

    /// <summary>
    /// 压缩图片字节内存缓存的容量上限。桌面默认 64MB，移动端和浏览器默认 32MB。
    /// 设置为 0 可禁用压缩字节内存缓存，磁盘缓存不受影响。
    /// </summary>
    public long MemoryCapacityBytes
    {
        get
        {
            lock (_memoryGate)
            {
                return _memoryCapacityBytes;
            }
        }
        set
        {
            lock (_memoryGate)
            {
                _memoryCapacityBytes = Math.Max(0, value);
                TrimMemoryCache();
            }
        }
    }

    /// <summary>
    /// 是否启用物理磁盘缓存。关闭后请求的 Disk/MemoryAndDisk 模式不会读写磁盘，
    /// 其中 MemoryAndDisk 会自动退化为仅内存缓存。浏览器平台默认关闭。
    /// </summary>
    public bool IsDiskCacheEnabled
    {
        get => Volatile.Read(ref _isDiskCacheEnabled) != 0;
        set => Volatile.Write(ref _isDiskCacheEnabled, value && !OperatingSystem.IsBrowser() ? 1 : 0);
    }

    /// <summary>
    /// 物理磁盘缓存容量上限。桌面默认 256MB，移动端默认 128MB。
    /// 设置为 0 等同于关闭物理缓存；缩小容量后会在后台自动回收旧文件。
    /// </summary>
    public long DiskCapacityBytes
    {
        get => Interlocked.Read(ref _diskCapacityBytes);
        set
        {
            long capacity = Math.Max(0, value);
            Interlocked.Exchange(ref _diskCapacityBytes, capacity);
            if (capacity == 0)
            {
                IsDiskCacheEnabled = false;
            }
            ScheduleDiskMaintenance(force: true);
        }
    }

    /// <summary>
    /// 当前应用的默认物理缓存目录。桌面按入口应用名隔离；Android/iOS 使用应用沙箱缓存目录。
    /// </summary>
    public string DefaultDiskCacheDirectory => _defaultDiskCacheDirectory;

    public async Task<byte[]?> GetAsync(string key, LuminaImageLoadOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (options.CacheMode is LuminaImageCacheMode.Memory or LuminaImageCacheMode.MemoryAndDisk
            && TryGetMemoryEntry(key, out byte[]? memoryBytes))
        {
            return memoryBytes;
        }
        if (IsDiskCacheEnabled
            && DiskCapacityBytes > 0
            && options.CacheMode is LuminaImageCacheMode.Disk or LuminaImageCacheMode.MemoryAndDisk
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
                        SetMemoryEntry(key, cachedBytes, options.CacheDuration);
                    }
                    TouchCacheFile(cacheFile);
                    ScheduleDiskMaintenance(force: Interlocked.Read(ref _estimatedDiskSizeBytes) < 0);
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
            SetMemoryEntry(key, bytes, options.CacheDuration);
        }
        if (!IsDiskCacheEnabled
            || DiskCapacityBytes <= 0
            || options.CacheMode is not (LuminaImageCacheMode.Disk or LuminaImageCacheMode.MemoryAndDisk)
            || !TryGetCacheFilePath(key, options.CacheDirectory, out string cacheFile))
        {
            return;
        }
        SemaphoreSlim diskLock = DiskLocks.GetLock(cacheFile);
        await diskLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            long previousLength = TryGetFileLength(cacheFile);
            await WriteCacheFileAtomicAsync(cacheFile, bytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            long estimatedSize = Interlocked.Read(ref _estimatedDiskSizeBytes);
            if (estimatedSize >= 0)
            {
                estimatedSize = Interlocked.Add(ref _estimatedDiskSizeBytes, bytes.LongLength - previousLength);
            }
            ScheduleDiskMaintenance(force: estimatedSize < 0 || estimatedSize > DiskCapacityBytes);
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
        lock (_memoryGate)
        {
            _memoryMap.Clear();
            _memoryLru.Clear();
            _memorySizeBytes = 0;
        }
    }

    /// <summary>
    /// 清空此缓存实例已经使用过的全部物理图片缓存目录。
    /// </summary>
    public async Task ClearDiskCacheAsync(CancellationToken cancellationToken = default)
    {
        await _diskMaintenanceLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            foreach (string directory in GetDiskDirectoriesSnapshot())
            {
                foreach (string file in EnumerateOwnedCacheFiles(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SemaphoreSlim diskLock = DiskLocks.GetLock(file);
                    await diskLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    try
                    {
                        TryDeleteFile(file);
                    }
                    finally
                    {
                        diskLock.Release();
                    }
                }
            }
            Interlocked.Exchange(ref _estimatedDiskSizeBytes, 0);
        }
        finally
        {
            _diskMaintenanceLock.Release();
        }
    }

    /// <summary>
    /// 立即按当前容量上限回收此缓存实例使用过的旧缓存，并返回回收后的字节数。
    /// </summary>
    public async Task<long> TrimDiskCacheAsync(CancellationToken cancellationToken = default)
    {
        await _diskMaintenanceLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            return await TrimDiskCacheCoreAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        finally
        {
            _diskMaintenanceLock.Release();
        }
    }

    private bool TryGetMemoryEntry(string key, out byte[]? bytes)
    {
        lock (_memoryGate)
        {
            if (!_memoryMap.TryGetValue(key, out LinkedListNode<MemoryEntry>? node))
            {
                bytes = null;
                return false;
            }

            if (node.Value.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                RemoveMemoryNode(node);
                bytes = null;
                return false;
            }

            _memoryLru.Remove(node);
            _memoryLru.AddFirst(node);
            bytes = node.Value.Bytes;
            return true;
        }
    }

    private void SetMemoryEntry(string key, byte[] bytes, TimeSpan duration)
    {
        lock (_memoryGate)
        {
            if (_memoryMap.TryGetValue(key, out LinkedListNode<MemoryEntry>? existing))
            {
                RemoveMemoryNode(existing);
            }

            if (_memoryCapacityBytes <= 0 || bytes.LongLength > _memoryCapacityBytes)
            {
                return;
            }

            MemoryEntry entry = new(key, bytes, DateTimeOffset.UtcNow.Add(duration));
            LinkedListNode<MemoryEntry> node = new(entry);
            _memoryLru.AddFirst(node);
            _memoryMap[key] = node;
            _memorySizeBytes += bytes.LongLength;
            TrimMemoryCache();
        }
    }

    private void TrimMemoryCache()
    {
        while (_memorySizeBytes > _memoryCapacityBytes && _memoryLru.Last is { } last)
        {
            RemoveMemoryNode(last);
        }
    }

    private void RemoveMemoryNode(LinkedListNode<MemoryEntry> node)
    {
        _memoryLru.Remove(node);
        _memoryMap.Remove(node.Value.Key);
        _memorySizeBytes -= node.Value.Bytes.LongLength;
    }

    private static long GetDefaultMemoryCapacityBytes()
    {
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser()
            ? 32L * Megabyte
            : 64L * Megabyte;
    }

    private static long GetDefaultDiskCapacityBytes()
    {
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
            ? 128L * Megabyte
            : 256L * Megabyte;
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
            TryDeleteFile(cacheFile);
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

    private bool TryGetCacheFilePath(string key, string? cacheDirectory, out string cacheFile)
    {
        cacheFile = string.Empty;
        try
        {
            string root = string.IsNullOrWhiteSpace(cacheDirectory) ? _defaultDiskCacheDirectory : cacheDirectory;
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }
            root = Path.GetFullPath(root);
            Directory.CreateDirectory(root);
            RegisterDiskDirectory(root);
            cacheFile = Path.Combine(root, Hash(key) + ".img");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ScheduleDiskMaintenance(bool force = false)
    {
        if (OperatingSystem.IsBrowser()
            || (!force && DateTime.UtcNow.Ticks < Interlocked.Read(ref _nextDiskMaintenanceUtcTicks)))
        {
            return;
        }
        if (Interlocked.Exchange(ref _diskMaintenanceScheduled, 1) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await TrimDiskCacheAsync().ConfigureAwait(continueOnCapturedContext: false);
                Interlocked.Exchange(ref _nextDiskMaintenanceUtcTicks, DateTime.UtcNow.AddHours(24).Ticks);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LuminaImageCache: failed to maintain disk cache: {ex}");
            }
            finally
            {
                Volatile.Write(ref _diskMaintenanceScheduled, 0);
            }
        });
    }

    private async Task<long> TrimDiskCacheCoreAsync(CancellationToken cancellationToken)
    {
        List<DiskEntry> entries = new();
        long totalBytes = 0;

        foreach (string directory in GetDiskDirectoriesSnapshot())
        {
            foreach (string file in EnumerateCacheFiles(directory, "*.img"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    FileInfo info = new(file);
                    if (!info.Exists)
                    {
                        continue;
                    }
                    entries.Add(new DiskEntry(file, info.Length, info.LastWriteTimeUtc));
                    totalBytes += info.Length;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        long capacity = DiskCapacityBytes;
        if (totalBytes <= capacity)
        {
            Interlocked.Exchange(ref _estimatedDiskSizeBytes, totalBytes);
            return totalBytes;
        }

        long targetBytes = (long)(capacity * 0.85);
        entries.Sort(static (left, right) => left.LastWriteTimeUtc.CompareTo(right.LastWriteTimeUtc));
        foreach (DiskEntry entry in entries)
        {
            if (totalBytes <= targetBytes)
            {
                break;
            }

            SemaphoreSlim diskLock = DiskLocks.GetLock(entry.Path);
            await diskLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            try
            {
                if (File.Exists(entry.Path))
                {
                    TryDeleteFile(entry.Path);
                    if (!File.Exists(entry.Path))
                    {
                        totalBytes -= entry.Length;
                    }
                }
            }
            finally
            {
                diskLock.Release();
            }
        }

        totalBytes = Math.Max(0, totalBytes);
        Interlocked.Exchange(ref _estimatedDiskSizeBytes, totalBytes);
        return totalBytes;
    }

    private string[] GetDiskDirectoriesSnapshot()
    {
        lock (_diskDirectoriesGate)
        {
            return _diskDirectories.ToArray();
        }
    }

    private void RegisterDiskDirectory(string directory)
    {
        lock (_diskDirectoriesGate)
        {
            if (_diskDirectories.Add(directory))
            {
                Interlocked.Exchange(ref _estimatedDiskSizeBytes, -1);
            }
        }
    }

    private static IEnumerable<string> EnumerateOwnedCacheFiles(string directory)
    {
        return EnumerateCacheFiles(directory, "*.img")
            .Concat(EnumerateCacheFiles(directory, "*.tmp"));
    }

    private static IEnumerable<string> EnumerateCacheFiles(string directory, string pattern = "*")
    {
        try
        {
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).ToArray()
                : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static void TouchCacheFile(string cacheFile)
    {
        try
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(cacheFile);
            if (DateTime.UtcNow - lastWrite >= TimeSpan.FromHours(24))
            {
                File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow);
            }
        }
        catch
        {
        }
    }

    private static long TryGetFileLength(string file)
    {
        try
        {
            return File.Exists(file) ? new FileInfo(file).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string ResolveDefaultDiskCacheDirectory()
    {
        if (OperatingSystem.IsBrowser())
        {
            return string.Empty;
        }

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return Path.Combine(Path.GetTempPath(), "Images");
        }

        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.GetTempPath();
        }
        return Path.Combine(root, ResolveApplicationName(), "Cache", "Images");
    }

    private static string ResolveApplicationName()
    {
        string name = Assembly.GetEntryAssembly()?.GetName().Name
            ?? AppDomain.CurrentDomain.FriendlyName
            ?? "Application";
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }
        return string.IsNullOrWhiteSpace(name) ? "Application" : name;
    }

    private static string Hash(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
