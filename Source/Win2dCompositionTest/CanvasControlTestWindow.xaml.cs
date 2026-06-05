using System;
using System.Diagnostics;
using System.Numerics;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI;

namespace Win2dCompositionTest;

/// <summary>
/// A Win2D window using the non-animated <see cref="CanvasControl"/>. Animation is driven by
/// subscribing to <see cref="CompositionTarget.Rendering"/> and invalidating the canvas every
/// composition frame. Extends content into the title bar and uses an acrylic backdrop.
/// The animation is just a ball orbiting the centre.
/// </summary>
public sealed partial class CanvasControlTestWindow : Window
{
    private double _angle;
    private TimeSpan _lastRenderTime = TimeSpan.Zero;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private readonly Color _colGreen = Color.FromArgb(255, 0, 255, 136);
    private readonly Color _colGray = Color.FromArgb(255, 100, 100, 100);

    public CanvasControlTestWindow()
    {
        InitializeComponent();

        // Make the window borderless-ish: draw behind the title bar buttons.
        var titleBar = AppWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.ButtonBackgroundColor = Colors.Transparent;

        AnimationCanvas.Draw += OnDraw;

        // CanvasControl has no built-in loop; drive redraws from the composition clock.
        CompositionTarget.Rendering += OnRendering;
        AnimationCanvas.Unloaded += (_, _) => CompositionTarget.Rendering -= OnRendering;
    }

    // Advance the angle using elapsed time, then request a redraw.
    private void OnRendering(object? sender, object e)
    {
        var now = _stopwatch.Elapsed;
        if (_lastRenderTime != TimeSpan.Zero)
        {
            var dt = (now - _lastRenderTime).TotalSeconds;
            _angle = (_angle + 1.8 * dt) % (Math.PI * 2);
        }
        _lastRenderTime = now;

        AnimationCanvas.Invalidate();
    }

    // DRAW: render the orbit and the moving ball.
    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var center = new Vector2((float)sender.ActualWidth / 2f, (float)sender.ActualHeight / 2f);
        float orbitR = MathF.Min(center.X, center.Y) * 0.6f;

        // Orbit ring
        ds.DrawCircle(center, orbitR, _colGray, 1.0f);

        // Moving ball
        var ballPos = center + new Vector2(orbitR * MathF.Cos((float)_angle), orbitR * MathF.Sin((float)_angle));
        ds.DrawLine(center, ballPos, _colGray, 1.0f);
        ds.FillCircle(ballPos, 18f, _colGreen);
    }
}
