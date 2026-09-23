using System.Text;
using ZXing;
using ZXing.Common;

namespace CashTracker.Infrastructure.Services;

public static class MarketplaceQrSvgRenderer
{
    public static string Render(string content, int moduleSize = 6, string accessibleLabel = "Sevkiyat QR kodu")
    {
        var matrix = new MultiFormatWriter().encode(
            content,
            BarcodeFormat.QR_CODE,
            33,
            33,
            new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.CHARACTER_SET] = "UTF-8",
                [EncodeHintType.MARGIN] = 2,
                [EncodeHintType.ERROR_CORRECTION] = ZXing.QrCode.Internal.ErrorCorrectionLevel.M
            });
        var size = matrix.Width * moduleSize;
        var label = System.Security.SecurityElement.Escape(accessibleLabel) ?? string.Empty;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {size} {size}\" role=\"img\" aria-label=\"{label}\" shape-rendering=\"crispEdges\"><rect width=\"100%\" height=\"100%\" fill=\"white\"/><path fill=\"black\" d=\"");
        for (var y = 0; y < matrix.Height; y++)
        for (var x = 0; x < matrix.Width; x++)
            if (matrix[x, y])
                svg.Append($"M{x * moduleSize} {y * moduleSize}h{moduleSize}v{moduleSize}h-{moduleSize}z");
        return svg.Append("\"/></svg>").ToString();
    }
}
