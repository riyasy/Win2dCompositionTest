using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics.DirectX;
using Windows.UI;

namespace Win2dCompositionTest;

/// <summary>
/// A Win2D window that drives a <see cref="CanvasSwapChainPanel"/> directly. We own the
/// <see cref="CanvasSwapChain"/> and run the Update/Draw loop on a dedicated thread, so we keep
/// the unlocked, monitor-synced frame rate of <c>CanvasAnimatedControl</c> (144/180 Hz on capable
/// hardware) while ALSO keeping the acrylic/Mica backdrop alive across resizes.
///
/// The trick: the swap chain is created with <see cref="CanvasAlphaMode.Premultiplied"/> and cleared
/// to <see cref="Colors.Transparent"/> every frame, so it composites through to the backdrop. On
/// resize we call <see cref="CanvasSwapChain.ResizeBuffers(float, float, float)"/> from the render
/// thread, so there is never an opaque stretched frame (which is what makes CanvasAnimatedControl
/// lose the backdrop mid-resize).
/// </summary>
public sealed partial class CanvasSwapChainTestWindow : Window
{
    private readonly CanvasDevice _device = new();
    private CanvasSwapChain? _swapChain;
    private Thread? _renderThread;
    private volatile bool _running;

    // Latest panel size / dpi requested by the UI thread, consumed by the render thread.
    private readonly object _sizeLock = new();
    private float _width;
    private float _height;
    private float _dpi = 96f;
    private bool _sizeDirty;

    private double _angle;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan _lastTime = TimeSpan.Zero;

    private readonly Color _colGreen = Color.FromArgb(255, 0, 255, 136);
    private readonly Color _colGray = Color.FromArgb(255, 100, 100, 100);

    public CanvasSwapChainTestWindow()
    {
        InitializeComponent();

        // Make the window borderless-ish: draw behind the title bar buttons.
        var titleBar = AppWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.ButtonBackgroundColor = Colors.Transparent;

        SwapChainPanel.SizeChanged += OnPanelSizeChanged;
        SwapChainPanel.Loaded += OnPanelLoaded;
        Closed += OnClosed;
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        _dpi = (float)(SwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0) * 96f;
        _width = Math.Max(1f, (float)SwapChainPanel.ActualWidth);
        _height = Math.Max(1f, (float)SwapChainPanel.ActualHeight);

        // Premultiplied alpha + transparent clear is what lets the backdrop show through.
        _swapChain = new CanvasSwapChain(
            _device,
            _width,
            _height,
            _dpi,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            CanvasAlphaMode.Premultiplied);

        SwapChainPanel.SwapChain = _swapChain;

        _running = true;
        _renderThread = new Thread(RenderLoop) { IsBackground = true, Name = "Win2D SwapChain Render" };
        _renderThread.Start();
    }

    private void OnPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Record the new size; the render thread performs the actual ResizeBuffers.
        lock (_sizeLock)
        {
            _dpi = (float)(SwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0) * 96f;
            _width = Math.Max(1f, (float)e.NewSize.Width);
            _height = Math.Max(1f, (float)e.NewSize.Height);
            _sizeDirty = true;
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _running = false;
        _renderThread?.Join(500);
        _swapChain?.Dispose();
        _device.Dispose();
    }

    // Dedicated render thread: resize when needed, draw, then present (synced to vblank).
    private void RenderLoop()
    {
        while (_running && _swapChain is { } swapChain)
        {
            try
            {
                ApplyPendingResize(swapChain);
                Update();
                Draw(swapChain);

                // Present(1) blocks until the next vertical blank => runs at the monitor's
                // refresh rate (144/180 Hz on capable displays).
                swapChain.Present(1);
            }
            catch (Exception ex) when (_device.IsDeviceLost(ex.HResult))
            {
                // A real app would recreate the device here; for the test we just stop.
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private void ApplyPendingResize(CanvasSwapChain swapChain)
    {
        float w, h, dpi;
        lock (_sizeLock)
        {
            if (!_sizeDirty)
                return;
            _sizeDirty = false;
            w = _width;
            h = _height;
            dpi = _dpi;
        }
        swapChain.ResizeBuffers(w, h, dpi);
    }

    private void Update()
    {
        var now = _stopwatch.Elapsed;
        if (_lastTime != TimeSpan.Zero)
        {
            var dt = (now - _lastTime).TotalSeconds;
            _angle = (_angle + 1.8 * dt) % (Math.PI * 2);
        }
        _lastTime = now;
    }

    private void Draw(CanvasSwapChain swapChain)
    {
        // Clearing to Transparent keeps the acrylic/Mica visible behind the drawing.
        using var ds = swapChain.CreateDrawingSession(Colors.Transparent);

        var size = swapChain.Size;
        var center = new Vector2((float)size.Width / 2f, (float)size.Height / 2f);
        float orbitR = MathF.Min(center.X, center.Y) * 0.6f;

        ds.DrawCircle(center, orbitR, _colGray, 1.0f);

        var ballPos = center + new Vector2(orbitR * MathF.Cos((float)_angle), orbitR * MathF.Sin((float)_angle));
        ds.DrawLine(center, ballPos, _colGray, 1.0f);
        ds.FillCircle(ballPos, 18f, _colGreen);
    }
}
