using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public sealed class LuminaImageLoader : ILuminaImageLoader
{
    private static readonly HttpClient SharedHttpClient = new HttpClient();

    private static readonly LuminaAsyncLockStripes WebLoadLocks = new LuminaAsyncLockStripes(64);

    public static LuminaImageLoader Shared { get; } = new LuminaImageLoader();

    public HttpClient HttpClient { get; set; }

    public ILuminaImageCache Cache { get; set; }

    public static LuminaImageLoader WithHttpClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient, "httpClient");
        return new LuminaImageLoader(httpClient);
    }

    public static LuminaImageLoader WithCache(ILuminaImageCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache, "cache");
        return new LuminaImageLoader(null, cache);
    }

    public static LuminaImageLoader WithHttpClientAndCache(HttpClient httpClient, ILuminaImageCache cache)
    {
        ArgumentNullException.ThrowIfNull(httpClient, "httpClient");
        ArgumentNullException.ThrowIfNull(cache, "cache");
        return new LuminaImageLoader(httpClient, cache);
    }

    public LuminaImageLoader()
        : this(SharedHttpClient, new LuminaImageCache())
    {
    }

    public LuminaImageLoader(HttpClient? httpClient, ILuminaImageCache? cache = null)
    {
        HttpClient = httpClient ?? SharedHttpClient;
        Cache = cache ?? new LuminaImageCache();
    }

    public async Task<IImage?> LoadAsync(object? source, LuminaImageLoadOptions options, CancellationToken cancellationToken)
    {
        if (source == null)
        {
            return null;
        }
        if (source is IImage image)
        {
            return image;
        }
        if (source is byte[] bytes)
        {
            return Decode(bytes);
        }
        string? sourceText = source switch
        {
            Uri uri => uri.ToString(),
            string s => s,
            _ => source.ToString()
        };
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return null;
        }
        sourceText = sourceText.Trim();
        if (TryDecodeBase64(sourceText, out byte[] base64Bytes))
        {
            return Decode(base64Bytes);
        }
        if (IsWebUri(sourceText))
        {
            return await LoadWebImageAsync(sourceText, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        return LoadLocalImage(sourceText);
    }

    public void ClearMemoryCache()
    {
        Cache.Clear();
    }

    private async Task<IImage?> LoadWebImageAsync(string url, LuminaImageLoadOptions options, CancellationToken cancellationToken)
    {
        return Decode(await ReadWebBytesAsync(url, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
    }

    private async Task<byte[]> ReadWebBytesAsync(string url, LuminaImageLoadOptions options, CancellationToken cancellationToken)
    {
        if (options.CacheMode == LuminaImageCacheMode.None)
        {
            return await HttpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
        byte[]? cachedBytes = await Cache.GetAsync(url, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (cachedBytes != null)
        {
            return cachedBytes;
        }
        SemaphoreSlim webLock = WebLoadLocks.GetLock(url);
        await webLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        try
        {
            byte[]? cachedBytesAfterWait = await Cache.GetAsync(url, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (cachedBytesAfterWait != null)
            {
                return cachedBytesAfterWait;
            }
            byte[] downloadedBytes = await HttpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await Cache.SetAsync(url, downloadedBytes, options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return downloadedBytes;
        }
        finally
        {
            webLock.Release();
        }
    }

    private static IImage? LoadLocalImage(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) && (uri.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals("resm", StringComparison.OrdinalIgnoreCase)))
        {
            using (Stream stream = AssetLoader.Open(uri))
            {
                return Decode(stream);
            }
        }
        if (File.Exists(source))
        {
            using (FileStream stream = File.OpenRead(source))
            {
                return Decode(stream);
            }
        }
        if (Uri.TryCreate(source, UriKind.Absolute, out uri) && uri.IsFile && File.Exists(uri.LocalPath))
        {
            using (FileStream stream = File.OpenRead(uri.LocalPath))
            {
                return Decode(stream);
            }
        }
        return null;
    }

    private static Bitmap? Decode(byte[] bytes)
    {
        using MemoryStream stream = new MemoryStream(bytes);
        return Decode(stream);
    }

    private static Bitmap? Decode(Stream stream)
    {
        return new Bitmap(stream);
    }

    private static bool TryDecodeBase64(string source, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        string payload = source;
        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int commaIndex = source.IndexOf(',');
            if (commaIndex < 0)
            {
                return false;
            }
            string metadata = source.Substring(0, commaIndex);
            if (!metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            int payloadStart = commaIndex + 1;
            payload = source.Substring(payloadStart, source.Length - payloadStart);
        }
        else if (source.Contains("://", StringComparison.Ordinal) || source.StartsWith("avares:", StringComparison.OrdinalIgnoreCase) || source.StartsWith("resm:", StringComparison.OrdinalIgnoreCase) || source.Length < 32)
        {
            return false;
        }
        try
        {
            payload = NormalizeBase64Payload(payload);
            bytes = Convert.FromBase64String(payload);
            return bytes.Length != 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeBase64Payload(string payload)
    {
        payload = string.Concat(payload.Where(c => !char.IsWhiteSpace(c))).Replace('-', '+').Replace('_', '/');
        int padding = payload.Length % 4;
        return (padding == 0) ? payload : payload.PadRight(payload.Length + 4 - padding, '=');
    }

    private static bool IsWebUri(string source)
    {
        Uri? uri;
        return Uri.TryCreate(source, UriKind.Absolute, out uri) && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }
}
