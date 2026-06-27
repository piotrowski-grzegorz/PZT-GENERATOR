using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PztGenerator;

internal static class RibbonIconFactory
{
    public static ImageSource Create(string text, Color background)
    {
        const int size = 32;
        var visual = new DrawingVisual();

        using (DrawingContext context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(background), null, new Rect(0, 0, size, size));
            context.DrawRectangle(null, new Pen(Brushes.White, 2), new Rect(2, 2, size - 4, size - 4));

            var formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                text.Length > 1 ? 10 : 16,
                Brushes.White,
                1.25);

            context.DrawText(
                formattedText,
                new Point((size - formattedText.Width) / 2, (size - formattedText.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
