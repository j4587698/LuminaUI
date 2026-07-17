using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace LuminaUI.Services;

/// <summary>
/// 解码后位图的内存缓存（LRU）。缓存的是已经按目标尺寸降采样解码的 <see cref="Bitmap"/>，
/// 而不是压缩字节，因此命中时可直接复用、零解码开销。
/// key 由「来源标识 + 目标解码尺寸」组成，保证不同尺寸需求各自缓存且不互相污染。
/// </summary>
/// <remarks>
/// 淘汰时<strong>不</strong>主动 Dispose 位图：同一个 <see cref="Bitmap"/> 实例可能仍被某个
/// 正在显示的控件引用，主动释放会导致黑屏或访问已释放的 native 资源。降采样后的位图占用很小，
/// 交由 GC 回收（Avalonia 的 Bitmap 终结器会释放底层资源）即可。
/// </remarks>
public sealed class LuminaDecodedImageCache
{
    private readonly object _gate = new();

    private readonly Dictionary<string, LinkedListNode<Entry>> _map = new(StringComparer.Ordinal);

    private readonly LinkedList<Entry> _lru = new();

    private int _capacity;

    private long _capacityBytes;

    private long _sizeBytes;

    public LuminaDecodedImageCache(int capacity = 256, long? capacityBytes = null)
    {
        _capacity = capacity < 1 ? 1 : capacity;
        _capacityBytes = Math.Max(1, capacityBytes ?? GetDefaultCapacityBytes());
    }

    /// <summary>
    /// 缓存可保留的已解码位图数量上限。超出后按最近最少使用淘汰。
    /// </summary>
    public int Capacity
    {
        get => _capacity;
        set
        {
            int normalized = value < 1 ? 1 : value;
            lock (_gate)
            {
                _capacity = normalized;
                TrimToCapacity();
            }
        }
    }

    /// <summary>
    /// 已解码像素的估算内存上限。桌面默认 64MB，移动端和浏览器默认 32MB。
    /// </summary>
    public long CapacityBytes
    {
        get => _capacityBytes;
        set
        {
            long normalized = Math.Max(1, value);
            lock (_gate)
            {
                _capacityBytes = normalized;
                TrimToCapacity();
            }
        }
    }

    public static string BuildKey(string source, int decodeWidth, int decodeHeight)
    {
        return string.Concat(source, "|w", decodeWidth.ToString(), "h", decodeHeight.ToString());
    }

    public bool TryGet(string key, out IImage image)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out LinkedListNode<Entry>? node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                image = node.Value.Image;
                return true;
            }
        }

        image = null!;
        return false;
    }

    public void Set(string key, IImage image)
    {
        if (image == null)
        {
            return;
        }

        lock (_gate)
        {
            if (_map.TryGetValue(key, out LinkedListNode<Entry>? existing))
            {
                _lru.Remove(existing);
                _sizeBytes -= existing.Value.SizeBytes;
                existing.Value = new Entry(key, image, EstimateSizeBytes(image));
                _sizeBytes += existing.Value.SizeBytes;
                _lru.AddFirst(existing);
            }
            else
            {
                LinkedListNode<Entry> node = new(new Entry(key, image, EstimateSizeBytes(image)));
                _lru.AddFirst(node);
                _map[key] = node;
                _sizeBytes += node.Value.SizeBytes;
            }

            TrimToCapacity();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _map.Clear();
            _lru.Clear();
            _sizeBytes = 0;
        }
    }

    private void TrimToCapacity()
    {
        while ((_lru.Count > _capacity || _sizeBytes > _capacityBytes) && _lru.Last is { } last)
        {
            _lru.RemoveLast();
            _map.Remove(last.Value.Key);
            _sizeBytes -= last.Value.SizeBytes;
        }
    }

    private static long EstimateSizeBytes(IImage image)
    {
        if (image is not Bitmap bitmap)
        {
            return 1;
        }

        try
        {
            return Math.Max(1, checked((long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4L));
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }

    private static long GetDefaultCapacityBytes()
    {
        const long megabyte = 1024L * 1024L;
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser()
            ? 32L * megabyte
            : 64L * megabyte;
    }

    private struct Entry
    {
        public Entry(string key, IImage image, long sizeBytes)
        {
            Key = key;
            Image = image;
            SizeBytes = sizeBytes;
        }

        public string Key { get; }

        public IImage Image { get; }

        public long SizeBytes { get; }
    }
}
