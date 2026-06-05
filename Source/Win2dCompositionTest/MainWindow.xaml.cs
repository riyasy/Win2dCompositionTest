using Microsoft.UI.Xaml;

namespace Win2dCompositionTest
{
    /// <summary>
    /// Landing window with two buttons, each opening a Win2D test window that
    /// extends content into the title bar and uses an acrylic backdrop.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Win2D Backdrop Test";

            // Set window size
            this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(600, 480));
        }

        private void OnAnimatedCanvasClicked(object sender, RoutedEventArgs e)
        {
            var window = new CanvasAnimatedControlTestWindow();
            window.Activate();
        }

        private void OnRenderingCanvasClicked(object sender, RoutedEventArgs e)
        {
            var window = new CanvasControlTestWindow();
            window.Activate();
        }

        private void OnSwapChainCanvasClicked(object sender, RoutedEventArgs e)
        {
            var window = new CanvasSwapChainTestWindow();
            window.Activate();
        }
    }
}
