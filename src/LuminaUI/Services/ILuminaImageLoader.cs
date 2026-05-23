using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace LuminaUI.Services;

public interface ILuminaImageLoader
{
	Task<IImage?> LoadAsync(object? source, LuminaImageLoadOptions options, CancellationToken cancellationToken);
}
