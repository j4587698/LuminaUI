using System.Threading;
using System.Threading.Tasks;

namespace LuminaUI.Services;

public interface ILuminaImageCache
{
	Task<byte[]?> GetAsync(string key, LuminaImageLoadOptions options, CancellationToken cancellationToken);

	Task SetAsync(string key, byte[] bytes, LuminaImageLoadOptions options, CancellationToken cancellationToken);

	void Clear();
}
