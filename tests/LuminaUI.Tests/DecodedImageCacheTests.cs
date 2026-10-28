using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LuminaUI.Services;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Pins the eviction contract of <see cref="LuminaDecodedImageCache"/>: an LRU bounded by both an
/// entry count and an estimated byte budget.
/// </summary>
/// <remarks>
/// <para>
/// The cache never throws when it misbehaves. Getting eviction wrong means either a slow memory
/// climb that nobody notices until a long session, or an image that has to be re-decoded on every
/// frame. The library has already been through a memory-leak fix and two rounds of image-loader
/// hardening, so the behaviour is worth nailing down.
/// </para>
/// <para>
/// These use real <see cref="WriteableBitmap"/> instances rather than a stub <see cref="IImage"/>:
/// <c>EstimateSizeBytes</c> falls back to 1 byte for anything that is not a <see cref="Bitmap"/>, so
/// a stub would silently make every byte-budget assertion vacuous.
/// </para>
/// </remarks>
public sealed class DecodedImageCacheTests
{
    private const int BitmapSide = 8;

    private const int BytesPerBitmap = BitmapSide * BitmapSide * 4;

    [AvaloniaFact]
    public void TryGetReportsAMissForAnUnknownKey()
    {
        LuminaDecodedImageCache cache = new();

        Assert.False(cache.TryGet("missing", out IImage image));
        Assert.Null(image);
    }

    [AvaloniaFact]
    public void TryGetReturnsTheCachedInstanceWithoutReDecoding()
    {
        LuminaDecodedImageCache cache = new();
        using WriteableBitmap bitmap = NewBitmap();

        cache.Set("a", bitmap);

        Assert.True(cache.TryGet("a", out IImage cached));
        Assert.Same(bitmap, cached);
    }

    [AvaloniaFact]
    public void CountCapacityEvictsTheOldestEntry()
    {
        LuminaDecodedImageCache cache = new(capacity: 2);
        using WriteableBitmap a = NewBitmap();
        using WriteableBitmap b = NewBitmap();
        using WriteableBitmap c = NewBitmap();

        cache.Set("a", a);
        cache.Set("b", b);
        cache.Set("c", c);

        Assert.False(cache.TryGet("a", out _), "the oldest entry is evicted");
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [AvaloniaFact]
    public void ReadingAnEntryProtectsItFromEviction()
    {
        LuminaDecodedImageCache cache = new(capacity: 2);
        using WriteableBitmap a = NewBitmap();
        using WriteableBitmap b = NewBitmap();
        using WriteableBitmap c = NewBitmap();

        cache.Set("a", a);
        cache.Set("b", b);

        Assert.True(cache.TryGet("a", out _)); // "a" becomes most recently used, "b" is now oldest

        cache.Set("c", c);

        Assert.True(cache.TryGet("a", out _), "a recently read entry survives");
        Assert.False(cache.TryGet("b", out _), "the least recently used entry is evicted");
        Assert.True(cache.TryGet("c", out _));
    }

    [AvaloniaFact]
    public void SettingAnExistingKeyReplacesTheValue()
    {
        LuminaDecodedImageCache cache = new();
        using WriteableBitmap first = NewBitmap();
        using WriteableBitmap second = NewBitmap();

        cache.Set("a", first);
        cache.Set("a", second);

        Assert.True(cache.TryGet("a", out IImage cached));
        Assert.Same(second, cached);
    }

    [AvaloniaFact]
    public void ByteBudgetEvictsOnceTheEstimateExceedsTheLimit()
    {
        LuminaDecodedImageCache cache = new(capacity: 16, capacityBytes: BytesPerBitmap * 2);
        using WriteableBitmap a = NewBitmap();
        using WriteableBitmap b = NewBitmap();
        using WriteableBitmap c = NewBitmap();

        cache.Set("a", a);
        cache.Set("b", b);

        Assert.True(cache.TryGet("a", out _), "two bitmaps fit exactly inside the budget");
        Assert.True(cache.TryGet("b", out _));

        cache.Set("c", c);

        Assert.False(cache.TryGet("a", out _), "the third bitmap pushes the oldest one out");
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [AvaloniaFact]
    public void AnEntryLargerThanTheWholeBudgetIsNotRetained()
    {
        // Guards the eviction loop: it has to terminate with an empty cache instead of spinning.
        LuminaDecodedImageCache cache = new(capacity: 16, capacityBytes: 1);
        using WriteableBitmap bitmap = NewBitmap();

        cache.Set("a", bitmap);

        Assert.False(cache.TryGet("a", out _));
    }

    [AvaloniaFact]
    public void LoweringTheCountCapacityTrimsImmediately()
    {
        LuminaDecodedImageCache cache = new(capacity: 3);
        using WriteableBitmap a = NewBitmap();
        using WriteableBitmap b = NewBitmap();
        using WriteableBitmap c = NewBitmap();

        cache.Set("a", a);
        cache.Set("b", b);
        cache.Set("c", c);

        cache.Capacity = 1;

        Assert.Equal(1, cache.Capacity);
        Assert.False(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _), "only the most recent entry survives");
    }

    [AvaloniaFact]
    public void LoweringTheByteBudgetTrimsImmediately()
    {
        LuminaDecodedImageCache cache = new(capacity: 16);
        using WriteableBitmap a = NewBitmap();
        using WriteableBitmap b = NewBitmap();
        using WriteableBitmap c = NewBitmap();

        cache.Set("a", a);
        cache.Set("b", b);
        cache.Set("c", c);

        cache.CapacityBytes = BytesPerBitmap;

        Assert.Equal(BytesPerBitmap, cache.CapacityBytes);
        Assert.False(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [AvaloniaFact]
    public void CapacitiesAreClampedToOne()
    {
        LuminaDecodedImageCache cache = new(capacity: 0, capacityBytes: 0);

        Assert.Equal(1, cache.Capacity);
        Assert.Equal(1, cache.CapacityBytes);

        cache.Capacity = -5;
        cache.CapacityBytes = -5;

        Assert.Equal(1, cache.Capacity);
        Assert.Equal(1, cache.CapacityBytes);
    }

    [AvaloniaFact]
    public void ClearDropsEveryEntry()
    {
        LuminaDecodedImageCache cache = new();
        using WriteableBitmap a = NewBitmap();

        cache.Set("a", a);
        cache.Clear();

        Assert.False(cache.TryGet("a", out _));
    }

    [AvaloniaFact]
    public void BuildKeySeparatesSourcesAndDecodeSizes()
    {
        Assert.NotEqual(
            LuminaDecodedImageCache.BuildKey("photo.png", 64, 64),
            LuminaDecodedImageCache.BuildKey("photo.png", 128, 128));

        Assert.NotEqual(
            LuminaDecodedImageCache.BuildKey("photo.png", 64, 64),
            LuminaDecodedImageCache.BuildKey("other.png", 64, 64));

        Assert.Equal(
            LuminaDecodedImageCache.BuildKey("photo.png", 64, 64),
            LuminaDecodedImageCache.BuildKey("photo.png", 64, 64));
    }

    [AvaloniaFact]
    public void ConcurrentUseDoesNotCorruptTheCache()
    {
        // Byte budget of exactly four bitmaps, matching the entry budget, so a drifting byte
        // counter shows up as premature evictions below.
        LuminaDecodedImageCache cache = new(capacity: 4, capacityBytes: 4L * BytesPerBitmap);
        using WriteableBitmap shared = NewBitmap();

        Parallel.For(0, 8, worker =>
        {
            for (int i = 0; i < 200; i++)
            {
                string key = $"w{worker}-{i % 16}";
                cache.Set(key, shared);
                cache.TryGet(key, out _);

                if (i % 50 == 0)
                {
                    cache.Clear();
                }
            }
        });

        // After the storm the cache must behave exactly like a fresh one: four new entries fit
        // (entry count and byte accounting are both consistent) and evict everything older.
        string[] fresh = ["final-0", "final-1", "final-2", "final-3"];
        foreach (string key in fresh)
        {
            cache.Set(key, shared);
        }

        foreach (string key in fresh)
        {
            Assert.True(cache.TryGet(key, out IImage cached), $"'{key}' was evicted: the size accounting drifted.");
            Assert.Same(shared, cached);
        }

        for (int worker = 0; worker < 8; worker++)
        {
            for (int i = 0; i < 16; i++)
            {
                Assert.False(cache.TryGet($"w{worker}-{i}", out _), $"Stale entry w{worker}-{i} survived past capacity.");
            }
        }

        // One more entry evicts exactly the least recently used one.
        cache.Set("final-4", shared);
        Assert.False(cache.TryGet("final-0", out _));
        Assert.True(cache.TryGet("final-4", out _));
    }

    private static WriteableBitmap NewBitmap() =>
        new(new PixelSize(BitmapSide, BitmapSide), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
}
