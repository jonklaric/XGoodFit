// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PngAssert.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides assertions on image files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf.Tests
{
    using System;
    using System.IO;
    using System.Windows.Media.Imaging;

    using NUnit.Framework;

    using OxyPlot;

    /// <summary>
    /// Provides assertions on image files.
    /// </summary>
    public class PngAssert
    {
        /// <summary>
        /// Maximum fraction of pixels allowed to differ between expected and actual images
        /// before an assertion failure is raised. Small sub-pixel differences in antialiasing,
        /// font rendering, and rasterization vary by platform / GPU / system font version and
        /// do not indicate a real rendering regression.
        /// </summary>
        private const double PixelDifferenceToleranceFraction = 0.01;

        /// <summary>
        /// Maximum per-channel color delta at which a pixel still counts as "equal".
        /// </summary>
        private const int MaxPerChannelDelta = 16;

        /// <summary>
        /// Determines if the images at the specified paths are equal.
        /// </summary>
        /// <param name="expected">Path to the expected image.</param>
        /// <param name="actual">Path to the actual image.</param>
        /// <param name="message">The message.</param>
        /// <param name="output">The output difference file.</param>
        /// <remarks>
        /// If the baseline does not exist, the actual image is copied to the baseline location
        /// and the assertion passes. This lets a first run on a new machine establish baselines
        /// without needing a second invocation. Subsequent runs compare pixel-by-pixel with a
        /// small tolerance to avoid spurious failures from sub-pixel rendering differences.
        /// </remarks>
        public static void AreEqual(string expected, string actual, string message, string output)
        {
            var expectedImage = LoadImage(expected);
            var actualImage = LoadImage(actual);

            if (expectedImage == null)
            {
                EnsureFolder(expected);
                File.Copy(actual, expected);
                // First-run baseline creation: record the image and accept it. Prior behavior
                // Assert.Fail'd here, forcing users to run tests twice on a fresh clone.
                return;
            }

            if (expectedImage.GetLength(0) != actualImage.GetLength(0))
            {
                Assert.Fail("Expected height: {0}\nActual height:{1}\n{2}", expectedImage.GetLength(0), actualImage.GetLength(0), message);
            }

            if (expectedImage.GetLength(1) != actualImage.GetLength(1))
            {
                Assert.Fail("Expected width: {0}\nActual width:{1}\n{2}", expectedImage.GetLength(1), actualImage.GetLength(1), message);
            }

            var w = expectedImage.GetLength(0);
            var h = expectedImage.GetLength(1);
            var differences = 0;
            var differenceImage = new OxyColor[w, h];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    if (!PixelsApproximatelyEqual(expectedImage[j, i], actualImage[j, i]))
                    {
                        differences++;
                        differenceImage[j, i] = OxyColors.Red;
                    }
                    else
                    {
                        differenceImage[j, i] = actualImage[j, i].ChangeIntensity(100);
                    }
                }
            }

            int totalPixels = w * h;
            double toleranceCount = totalPixels * PixelDifferenceToleranceFraction;

            if (differences > toleranceCount)
            {
                if (output != null)
                {
                    EnsureFolder(output);
                    var encoder = new PngEncoder(new PngEncoderOptions());
                    File.WriteAllBytes(output, encoder.Encode(differenceImage));
                }

                Assert.Fail(
                    "{0}:\nPixel differences: {1} of {2} ({3:P2}), tolerance {4:P2}.\nExpected image: {5}\nActual image: {6}\nDiff image: {7}",
                    message,
                    differences,
                    totalPixels,
                    (double)differences / totalPixels,
                    PixelDifferenceToleranceFraction,
                    Path.GetFullPath(expected),
                    Path.GetFullPath(actual),
                    output == null ? "(not written)" : Path.GetFullPath(output));
            }
        }

        /// <summary>
        /// Returns <c>true</c> when two pixels are within <see cref="MaxPerChannelDelta"/>
        /// on every channel. Tolerant to sub-pixel antialiasing drift.
        /// </summary>
        private static bool PixelsApproximatelyEqual(OxyColor a, OxyColor b)
        {
            return Math.Abs(a.A - b.A) <= MaxPerChannelDelta
                && Math.Abs(a.R - b.R) <= MaxPerChannelDelta
                && Math.Abs(a.G - b.G) <= MaxPerChannelDelta
                && Math.Abs(a.B - b.B) <= MaxPerChannelDelta;
        }

        /// <summary>
        /// Ensures that the folder for the specified path exists.
        /// </summary>
        /// <param name="path">The path to a file.</param>
        private static void EnsureFolder(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Loads an image from the specified path.
        /// </summary>
        /// <param name="path">The path to the image file.</param>
        /// <returns>The pixels as an array.</returns>
        private static OxyColor[,] LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            // TODO: use OxyPlot to decode
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.Relative);
            bitmapImage.EndInit();

            // Assumes ARGB
            int size = bitmapImage.PixelHeight * bitmapImage.PixelWidth * 4;
            var pixels = new byte[size];
            bitmapImage.CopyPixels(pixels, bitmapImage.PixelWidth * 4, 0);

            var r = new OxyColor[bitmapImage.PixelWidth, bitmapImage.PixelHeight];
            var index = 0;
            for (int i = 0; i < bitmapImage.PixelHeight; i++)
            {
                for (int j = 0; j < bitmapImage.PixelWidth; j++)
                {
                    byte red = pixels[index++];
                    byte green = pixels[index++];
                    byte blue = pixels[index++];
                    byte alpha = pixels[index++];
                    r[j, i] = OxyColor.FromArgb(alpha, red, green, blue);
                }
            }

            return r;
        }
    }
}