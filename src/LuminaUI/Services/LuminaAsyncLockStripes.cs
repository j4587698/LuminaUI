using System;
using System.Threading;

namespace LuminaUI.Services;

internal sealed class LuminaAsyncLockStripes
{
    private readonly SemaphoreSlim[] _locks;

    public LuminaAsyncLockStripes(int stripeCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stripeCount, "stripeCount");
        _locks = new SemaphoreSlim[stripeCount];
        for (int i = 0; i < _locks.Length; i++)
        {
            _locks[i] = new SemaphoreSlim(1, 1);
        }
    }

    public SemaphoreSlim GetLock(string key)
    {
        uint hash = (uint)StringComparer.Ordinal.GetHashCode(key);
        return _locks[hash % (uint)_locks.Length];
    }
}
