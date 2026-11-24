using System.IO;
using System.Net;
using System.Text;
using BankingManagesandServicesApp.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
namespace BankingManagesandServicesApp.Services;

public static class ReceiptExporter
{
    public static IReadOnlyList<(string Label, string Value)> Fields(Receipt receipt)
    {
        var t = receipt.Transaction;
        return new List<(string, string)>
        {
            ("Số chứng từ", receipt.Number), ("Mã giao dịch", t.Id), ("Thời gian giao dịch", t.TimeText),
            ("Tài khoản khách hàng", t.AccountNumber), ("Tên khách hàng", t.CustomerName),
            ("Loại giao dịch", t.TransactionType), ("Số tiền", t.AmountText), ("Trạng thái", t.Status),
            ("Nội dung", t.Description), ("Cảnh báo", t.IsFlagged ? t.FlagReason : "Không"),
            ("Người xuất chứng từ", receipt.Issuer.DisplayName), ("Vai trò", receipt.Issuer.RoleName),
            ("Thời điểm xuất", receipt.IssuedAtText)
        };
    }

    public static byte[] CreatePdf(Receipt receipt)
    {
        using var doc = new PdfDocument();
        doc.Info.Title = "Chứng từ giao dịch " + receipt.Transaction.Id;
        doc.Info.Author = receipt.Issuer.DisplayName;
        var body = new XFont("Arial", 11, XFontStyleEx.Regular);
        var bold = new XFont("Arial", 11, XFontStyleEx.Bold);
        var title = new XFont("Arial", 21, XFontStyleEx.Bold);
        var small = new XFont("Arial", 9, XFontStyleEx.Regular);
        var ink = new XSolidBrush(XColor.FromArgb(20,45,78));
        XGraphics gfx = null!;
        double y = 0;
        void NewPage()
        {
            gfx?.Dispose();
            var page = doc.AddPage(); page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            gfx.DrawRectangle(ink, 0, 0, page.Width.Point, 12);
            gfx.DrawString("BANKING / NHÓM 07", bold, ink, 44, 48);
            gfx.DrawString("CHỨNG TỪ GIAO DỊCH", title, ink, 44, 88);
            gfx.DrawString("BẢN DEMO ĐỒ ÁN - Không dùng làm chứng từ ngân hàng thực tế", small, XBrushes.Gray, 44, 112);
            gfx.DrawLine(XPens.LightGray, 44, 128, 551, 128);
            gfx.DrawString($"{receipt.Number} | Trang {doc.PageCount}", small, XBrushes.Gray, 44, 800);
            y = 146;
        }
        NewPage();
        foreach (var (label, value) in Fields(receipt))
        {
            var lines = Wrap(gfx, value, body, 335);
            double height = Math.Max(36, lines.Count * 17 + 16);
            if (y + height > 735) NewPage();
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(243,246,250)), 44, y, 507, height);
            gfx.DrawString(label, bold, ink, 54, y + 21);
            for (int i = 0; i < lines.Count; i++) gfx.DrawString(lines[i], body, XBrushes.Black, 205, y + 21 + 17 * i);
            y += height + 3;
        }
        if (y + 45 > 765) NewPage();
        gfx.DrawString("Thông tin được trích từ hệ thống tại thời điểm xuất.", small, XBrushes.Gray, 44, y + 25);
        gfx.Dispose();
        using var stream = new MemoryStream();
        doc.Save(stream, false);
        return stream.ToArray();
    }

    private static List<string> Wrap(XGraphics gfx, string text, XFont font, double width)
    {
        var lines = new List<string>();
        foreach (var paragraph in new[] { System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim() })
        {
            string line = "";
            foreach (char ch in paragraph)
            {
                if (gfx.MeasureString(line + ch, font).Width > width && line.Length > 0)
                { lines.Add(line); line = ""; }
                line += ch;
            }
            lines.Add(line);
        }
        return lines;
    }

    public static byte[] CreateHtml(Receipt receipt)
    {
        static string E(string s) => WebUtility.HtmlEncode(s);
        var rows = string.Join("", Fields(receipt).Select(row => $"<tr><th>{E(row.Label)}</th><td>{E(row.Value)}</td></tr>"));
        string html = $$$"""
            <!doctype html><html lang="vi"><head><meta charset="utf-8"><title>{{{E(receipt.Number)}}}</title>
            <style>body{font:15px 'Segoe UI',Arial,sans-serif;color:#142d4e;background:#f3f6fa;margin:0;padding:40px}
            main{max-width:800px;margin:auto;background:white;padding:40px;border-top:10px solid #142d4e}
            h1{font-size:27px}small,footer{color:#65758a}table{width:100%;border-collapse:collapse;margin:24px 0}
            th,td{text-align:left;padding:12px;border-bottom:1px solid #dce6f1;vertical-align:top;overflow-wrap:anywhere;white-space:pre-wrap}
            th{width:30%;background:#f3f6fa}button{padding:10px 20px;cursor:pointer}@media print{body{background:white;padding:0}main{padding:20px}button{display:none}}</style></head>
            <body><main><b>BANKING / NHÓM 07</b><h1>CHỨNG TỪ GIAO DỊCH</h1>
            <small>BẢN DEMO ĐỒ ÁN - Không dùng làm chứng từ ngân hàng thực tế</small>
            <table>{{{rows}}}</table><footer>Thông tin được trích từ hệ thống tại thời điểm xuất.</footer>
            <p><button onclick="window.print()">In chứng từ</button></p></main></body></html>
            """;
        return Encoding.UTF8.GetBytes(html);
    }
}
