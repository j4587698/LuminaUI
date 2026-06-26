using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace LuminaUI.Services;

public interface ILuminaImageLoader
{
    Task<IImage?> LoadAsync(object? source, LuminaImageLoadOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// 尝试从已解码的内存缓存中同步获取图像,命中时可避免一次异步加载与重新解码。
    /// 主要用于虚拟化列表项被回收/复用时立即复用已解码的位图,消除滚动闪烁。
    /// 默认实现返回 null（未命中），自定义加载器可按需重写。
    /// </summary>
    IImage? TryGetCachedImage(object? source, LuminaImageLoadOptions options) => null;
}
