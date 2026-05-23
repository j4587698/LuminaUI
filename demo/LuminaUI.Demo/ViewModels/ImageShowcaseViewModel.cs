using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Services;

namespace LuminaUI.Demo.ViewModels;

public partial class ImageShowcaseViewModel : ObservableObject
{
    private const string DemoImageBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAGAAAABACAYAAADlNHIOAAAAqklEQVR42u3RQRFAYBQGwBfCaCCFCGKI4C6DFDK4SyCHBE6/GN8we9gEW8N8t6Tu2KOepY8qAQIECBAgQIAAAQIECBAgQIAAAQIECBAgQIAAAQIECBAgQIAAAQIECBAgQIAAAQIECBAgQIAAAQIECBAgQIAAAQIECBAg4FsBNZ4talujrqmiBAgQIECAAAECBAgQIECAAAECBAgQIECAAAECBAgQIEDAfwNe59fd2lauCKYAAAAASUVORK5CYII=";

    public string WebImageUrl { get; } = "https://picsum.photos/seed/lumina-ui/900/540";

    public string CachedAvatarUrl { get; } = "https://picsum.photos/seed/lumina-avatar/300/300";

    public string Base64ImageData { get; } = $"data:image/png;base64,{DemoImageBase64}";

    public string AvaresImageUrl { get; } = "avares://LuminaUI.Demo/Assets/lumina-resource.png";

    public string CustomHttpClientImageUrl { get; } = "https://picsum.photos/seed/lumina-custom-http/900/540";

    public string CustomCacheImageUrl { get; } = "https://picsum.photos/seed/lumina-custom-cache/900/540";

    public ILuminaImageLoader AuthorizedHttpClientLoader { get; } =
        LuminaImageLoader.WithHttpClient(new HttpClient(new DemoAuthorizationHandler()));

    public ILuminaImageLoader CustomCacheLoader { get; } =
        LuminaImageLoader.WithCache(new DemoMemoryImageCache());

    public string BrokenImageUrl { get; } = "https://example.invalid/lumina-image.png";

    private sealed class DemoAuthorizationHandler : DelegatingHandler
    {
        public DemoAuthorizationHandler()
            : base(new HttpClientHandler())
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "demo-token");
            request.Headers.TryAddWithoutValidation("X-Lumina-Demo", "custom-client");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class DemoMemoryImageCache : ILuminaImageCache
    {
        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        public Task<byte[]?> GetAsync(
            string key,
            LuminaImageLoadOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_entries.TryGetValue(key, out var entry) &&
                entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return Task.FromResult<byte[]?>(entry.Bytes);
            }

            return Task.FromResult<byte[]?>(null);
        }

        public Task SetAsync(
            string key,
            byte[] bytes,
            LuminaImageLoadOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _entries[key] = new Entry(bytes, DateTimeOffset.UtcNow.Add(options.CacheDuration));
            return Task.CompletedTask;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private sealed record Entry(byte[] Bytes, DateTimeOffset ExpiresAt);
    }
}
