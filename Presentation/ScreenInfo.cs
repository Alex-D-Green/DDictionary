using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace DDictionary.Presentation
{
    //https://stackoverflow.com/a/2118993/2187280

    /// <summary>
    /// Helper class to get information about a screen bounds and etc.
    /// Also contains static methods to convert pixels to/from dpi coords.
    /// </summary>
    public sealed class ScreenInfo
    {
        private readonly Screen screen;


        public Rect ScreenBoundsPix { get => ConvertRect(screen.Bounds); }

        public Rect WorkingAreaPix { get => ConvertRect(screen.WorkingArea); }

        public bool IsPrimary { get => screen.Primary; }

        public string DeviceName { get => screen.DeviceName; }


        public static ScreenInfo PrimaryScreen { get => new ScreenInfo(Screen.PrimaryScreen); }


        public ScreenInfo(Screen screen)
        {
            this.screen = screen ?? throw new ArgumentNullException(nameof(screen));
        }


        private Rect ConvertRect(Rectangle val)
        {
            return new Rect {
                X = val.X,
                Y = val.Y,
                Width = val.Width,
                Height = val.Height
            };
        }


        public static IEnumerable<ScreenInfo> AllScreens()
        {
            return Screen.AllScreens.Select(o => new ScreenInfo(o));
        }

        public static ScreenInfo GetScreenFrom(Window window)
        {
            var windowInteropHelper = new WindowInteropHelper(window);
            var screen = Screen.FromHandle(windowInteropHelper.Handle);

            return new ScreenInfo(screen);
        }

        public static ScreenInfo GetScreenFrom(System.Windows.Point point)
        {
            var drawingPoint = new System.Drawing.Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
            var screen = Screen.FromPoint(drawingPoint);

            return new ScreenInfo(screen);
        }

        public static System.Windows.Point GetScreenPpi(Visual visual)
        {
            PresentationSource source = PresentationSource.FromVisual(visual) ?? 
                throw new ArgumentException("The visual is disposed.", nameof(visual));

            return new System.Windows.Point {
                X = 96 * source.CompositionTarget.TransformToDevice.M11,
                Y = 96 * source.CompositionTarget.TransformToDevice.M22
            };
        }

        #region Converters pixels -> dpi

        public static double GetXValueInDpi(Visual visual, double xPix)
        {
            var data = GetScreenPpi(visual);

            return xPix / data.X * 96;
        }

        public static double GetYValueInDpi(Visual visual, double yPix)
        {
            var data = GetScreenPpi(visual);

            return yPix / data.Y * 96;
        }

        public static System.Windows.Point GetPointInDpi(Visual visual, System.Windows.Point pointPix)
        {
            var data = GetScreenPpi(visual);

            return new System.Windows.Point(
                pointPix.X / data.X * 96, 
                pointPix.Y / data.Y * 96);
        }

        public static Rect GetRectInDpi(Visual visual, Rect rectPix)
        {
            var data = GetScreenPpi(visual);

            return new Rect(
                rectPix.Left / data.X * 96, 
                rectPix.Top / data.Y * 96,
                rectPix.Width / data.X * 96, 
                rectPix.Height / data.Y * 96);
        }

        #endregion

        #region Converters dpi -> pixels

        public static double GetXValueInPix(Visual visual, double xDpi)
        {
            var data = GetScreenPpi(visual);

            return xDpi / 96 * data.X;
        }

        public static double GetYValueInPix(Visual visual, double yDpi)
        {
            var data = GetScreenPpi(visual);

            return yDpi / 96 * data.Y;
        }

        public static System.Windows.Point GetPointInPix(Visual visual, System.Windows.Point pointDpi)
        {
            var data = GetScreenPpi(visual);

            return new System.Windows.Point(
                pointDpi.X / 96 * data.X,
                pointDpi.Y / 96 * data.Y);
        }

        public static Rect GetRectInPix(Visual visual, Rect rectDpi)
        {
            var data = GetScreenPpi(visual);

            return new Rect(
                rectDpi.Left / 96 * data.X,
                rectDpi.Top / 96 * data.Y,
                rectDpi.Width / 96 * data.X,
                rectDpi.Height / 96 * data.Y);
        }

        #endregion
    }
}
