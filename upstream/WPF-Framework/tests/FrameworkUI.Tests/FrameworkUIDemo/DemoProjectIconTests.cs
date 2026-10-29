using System.Windows.Media.Imaging;
using FrameworkUI.Demo;
using Xunit;

namespace FrameworkUI.Tests.FrameworkUIDemo;

/// <summary>Checks the demo project's packaged icon quality.</summary>
public class DemoProjectIconTests
{
    /// <summary>Verifies that the project image uses a frozen, square, high-resolution icon frame.</summary>
    [Fact]
    public void ProjectImage_LoadsLargestAvailableIconFrame()
    {
        DispatcherTestHost.Run(() =>
        {
            var image = Assert.IsAssignableFrom<BitmapSource>(DemoProject.GetInstance().ProjectImage);

            Assert.True(image.PixelWidth >= 128, $"Expected a high-resolution project icon, but got {image.PixelWidth}x{image.PixelHeight}.");
            Assert.Equal(image.PixelWidth, image.PixelHeight);
            Assert.True(image.IsFrozen);
        });
    }
}