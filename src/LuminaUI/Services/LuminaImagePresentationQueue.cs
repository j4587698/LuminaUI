using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using LuminaUI.Controls;

namespace LuminaUI.Services;

/// <summary>
/// 将解码完成的图片按渲染帧分批提交给控件，避免同一帧集中触发布局失效与纹理上传。
/// </summary>
internal static class LuminaImagePresentationQueue
{
    private readonly record struct PendingPresentation(
        LuminaImage Control,
        IImage Image,
        object? Source,
        int LoadVersion,
        CancellationToken CancellationToken);

    private sealed class TopLevelQueue
    {
        private readonly TopLevel _topLevel;

        private readonly Dictionary<LuminaImage, PendingPresentation> _pending = new();

        private readonly Queue<LuminaImage> _order = new();

        private bool _frameQueued;

        public TopLevelQueue(TopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        public void Enqueue(PendingPresentation presentation)
        {
            if (!_pending.ContainsKey(presentation.Control))
            {
                _order.Enqueue(presentation.Control);
            }

            _pending[presentation.Control] = presentation;
            QueueFrame();
        }

        private void QueueFrame()
        {
            if (_frameQueued || _order.Count == 0)
            {
                return;
            }

            _frameQueued = true;
            _topLevel.RequestAnimationFrame(ProcessFrame);
        }

        private void ProcessFrame(TimeSpan timestamp)
        {
            _frameQueued = false;
            int budget = GetFrameBudget();
            int queuedCount = _order.Count;

            while (budget > 0 && queuedCount-- > 0 && _order.TryDequeue(out LuminaImage? control))
            {
                if (!_pending.Remove(control, out PendingPresentation presentation))
                {
                    continue;
                }

                // 失效项直接丢弃，不占用本帧配额。
                if (control.TryPresentImage(
                    presentation.Image,
                    presentation.Source,
                    presentation.LoadVersion,
                    presentation.CancellationToken))
                {
                    budget--;
                }
            }

            QueueFrame();
        }
    }

    private static readonly object IncomingGate = new();

    private static readonly Dictionary<LuminaImage, PendingPresentation> Incoming = new();

    private static readonly ConditionalWeakTable<TopLevel, TopLevelQueue> TopLevelQueues = new();

    private static bool _drainQueued;

    public static void Enqueue(
        LuminaImage control,
        IImage image,
        object? source,
        int loadVersion,
        CancellationToken cancellationToken)
    {
        bool queueDrain = false;
        lock (IncomingGate)
        {
            Incoming[control] = new PendingPresentation(control, image, source, loadVersion, cancellationToken);
            if (!_drainQueued)
            {
                _drainQueued = true;
                queueDrain = true;
            }
        }

        if (queueDrain)
        {
            Dispatcher.UIThread.Post(DrainIncoming, DispatcherPriority.Render);
        }
    }

    private static void DrainIncoming()
    {
        PendingPresentation[] presentations;
        lock (IncomingGate)
        {
            presentations = Incoming.Values.ToArray();
            Incoming.Clear();
            _drainQueued = false;
        }

        foreach (PendingPresentation presentation in presentations)
        {
            if (!presentation.Control.CanPresentImage(
                presentation.Source,
                presentation.LoadVersion,
                presentation.CancellationToken))
            {
                continue;
            }

            TopLevel? topLevel = TopLevel.GetTopLevel(presentation.Control);
            if (topLevel != null)
            {
                TopLevelQueues.GetValue(topLevel, static owner => new TopLevelQueue(owner))
                    .Enqueue(presentation);
            }
        }
    }

    private static int GetFrameBudget()
    {
        return OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsBrowser()
            ? 1
            : 2;
    }
}
