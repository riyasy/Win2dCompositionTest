using System;
using System.Numerics;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Windows.UI;
using Microsoft.UI;

namespace Win2dCompositionTest;

/// <summary>
/// A Win2D window driven by <see cref="CanvasAnimatedControl"/>, which runs its own
/// Update/Draw loop on a background thread. Extends content into the title bar and uses
/// an acrylic backdrop. The animation is just a ball orbiting the centre.
/// </summary>
public sealed partial class CanvasAnimatedControlTestWindow : Window
{
    private double _angle;
    private readonly Color _colGreen = Color.FromArgb(255, 0, 255, 136);
    private readonly Color _colGray = Color.FromArgb(255, 100, 100, 100);

    public CanvasAnimatedControlTestWindow()
    {
        InitializeComponent();

        // Make the window borderless-ish: draw behind the title bar buttons.
        var titleBar = AppWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.ButtonBackgroundColor = Colors.Transparent;

        // CanvasAnimatedControl owns the Update + Draw loop internally.
        AnimationCanvas.Update += OnUpdate;
        AnimationCanvas.Draw += OnDraw;
    }

    // UPDATE LOOP: advance the orbit angle based on elapsed time.
    private void OnUpdate(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
    {
        _angle = (_angle + 1.8 * args.Timing.ElapsedTime.TotalSeconds) % (Math.PI * 2);
    }

    // DRAW LOOP: render the orbit and the moving ball.
    private void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var center = new Vector2((float)sender.Size.Width / 2f, (float)sender.Size.Height / 2f);
        float orbitR = MathF.Min(center.X, center.Y) * 0.6f;

        // Orbit ring
        ds.DrawCircle(center, orbitR, _colGray, 1.0f);

        // Moving ball
        var ballPos = center + new Vector2(orbitR * MathF.Cos((float)_angle), orbitR * MathF.Sin((float)_angle));
        ds.DrawLine(center, ballPos, _colGray, 1.0f);
        ds.FillCircle(ballPos, 18f, _colGreen);
    }
}
