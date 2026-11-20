using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BankingManagesandServicesApp;
using BankingManagesandServicesApp.DBManages;
using BankingManagesandServicesApp.GUIs;
using BankingManagesandServicesApp.Models;
using BankingManagesandServicesApp.Services;
using Microsoft.Data.SqlClient;
using PdfSharp.Pdf.IO;

internal static class Program
{
    private static readonly List<string> results = new();
    [STAThread]
    private static void Main()
    {
        var app = new App();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/BankingManagesandServicesApp;component/Theme.xaml", UriKind.Relative) });
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        app.Startup += async (_, _) =>
        {
            int code = 0;
            try { await RunAsync(app); }
            catch (Exception ex) { results.Add("FAIL: " + ex); code = 1; }
            Directory.CreateDirectory("output/verification");
            File.WriteAllLines("output/verification/smoke-results.txt", results);
            foreach (var line in results) Console.WriteLine(line);
            app.Shutdown(code);
        };
        app.Run();
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); results.Add("PASS: " + message); }
    private static void Capture(Window window, string name)
    {
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(window.Width, window.Height)); root.Arrange(new Rect(0,0,window.Width,window.Height)); root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.Width,(int)window.Height,96,96,PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var context = background.RenderOpen()) context.DrawRectangle(window.Background ?? new SolidColorBrush(Color.FromRgb(243,246,250)), null, new Rect(0,0,window.Width,window.Height));
        bitmap.Render(background);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create("output/verification/" + name + ".png"); encoder.Save(file);
    }
    private static async Task RunAsync(Application app)
    {
        Directory.CreateDirectory("output/verification");
        await DemoData.EnsureCreatedAsync();
        await DemoData.EnsureCreatedAsync();
        var repo = new BankRepository();
        Check(await repo.AuthenticateAsync(DemoData.Username,"wrong") == null,"Reject wrong password");
        Check(await repo.AuthenticateAsync("' OR 1=1--",DemoData.Password) == null,"Reject SQL injection login");
        var user = await repo.AuthenticateAsync(DemoData.Username,DemoData.Password);
        Check(user != null && user.CanExport,"Login and role export permission from SQL Server");
        Capture(new LoginAppGUI(),"01-login");
        UserSession.SignIn(user!);
        var transactions = await repo.SearchTransactionsAsync("DEMO-","",null,null);
        Check(transactions.Count == 4,"Demo setup is idempotent: four transactions");
        Check((await repo.SearchTransactionsAsync("DEMO-","Thành công",null,null)).Count == 2,"Filter successful transactions");
        Check((await repo.SearchTransactionsAsync("DEMO-","",DateTime.Today.AddDays(1),DateTime.Today.AddDays(2))).Count == 0,"Filter date range");
        Check((await repo.SearchTransactionsAsync("no-match","",null,null)).Count == 0,"Empty search result");
        var main = new TransactionLogGUI();
        main.Show();
        var grid = (System.Windows.Controls.DataGrid)main.FindName("TransactionsGrid");
        for (int i=0; i<50 && grid.Items.Count==0; i++) await Task.Delay(100);
        Check(grid.Items.Count>=4,"Main window loads SQL data on opening");
        ((System.Windows.Controls.DataGrid)main.FindName("TransactionsGrid")).SelectedItem = transactions.Single(t=>t.Id=="DEMO-GD001");
        await Task.Delay(200);
        Capture(main,"02-transactions");
        var successful = transactions.Single(t=>t.Id=="DEMO-GD001");
        var preview = new ReceiptPreviewGUI(successful);
        preview.Show(); await Task.Delay(100);
        Capture(preview,"03-receipt");
        preview.Close();
        var overview = new AccountBankingManageOverviewGUI(); overview.Show(); await Task.Delay(250);
        Capture(overview,"04-accounts"); overview.Close();
        var service = new ReceiptService();
        int before = (await repo.GetLogsAsync(successful.Id)).Count;
        Check(await service.ExportAsync(successful.Id,"output/verification/receipt-demo.pdf","PDF") == null,"Save PDF and audit logs");
        Check(await service.ExportAsync(successful.Id,"output/verification/receipt-demo.html","HTML") == null,"Save HTML and audit logs");
        Check((await repo.GetLogsAsync(successful.Id)).Count == before+2,"One transaction log per successful export");
        using (var pdf = PdfReader.Open("output/verification/receipt-demo.pdf",PdfDocumentOpenMode.Import)) Check(pdf.PageCount == 1,"Demo PDF has one A4 page");
        string html = Encoding.UTF8.GetString(ReceiptExporter.CreateHtml(new Receipt(successful with { Description="<script>alert('x')</script>" },user!,DateTime.Now)));
        Check(html.Contains("&lt;script&gt;"),"HTML escapes database text");
        try { await service.ExportAsync("DEMO-GD003","output/verification/should-not-exist.pdf","PDF"); throw new Exception("Pending export was accepted"); }
        catch (InvalidOperationException) { Check(!File.Exists("output/verification/should-not-exist.pdf"),"Pending transaction cannot issue receipt"); }
        try { await service.ExportAsync("DEMO-GD004","output/verification/should-not-exist.pdf","PDF"); throw new Exception("Failed export was accepted"); }
        catch (InvalidOperationException) { Check(!File.Exists("output/verification/should-not-exist.pdf"),"Failed transaction cannot issue receipt"); }
        int count = (await repo.GetLogsAsync(successful.Id)).Count;
        try { await service.ExportAsync(successful.Id,"output/verification/missing-folder/test.pdf","PDF"); throw new Exception("Invalid path was accepted"); }
        catch (DirectoryNotFoundException) { Check((await repo.GetLogsAsync(successful.Id)).Count==count,"File failure does not record successful export"); }
        UserSession.SignIn(user! with { AccountId = -123 });
        try { await service.ExportAsync(successful.Id,"output/verification/unauthorized.pdf","PDF"); throw new Exception("Unauthorized export accepted"); }
        catch (InvalidOperationException) { Check(!File.Exists("output/verification/unauthorized.pdf"),"Database rechecks authorization"); }
        UserSession.SignIn(user!);
        var longReceipt = new Receipt(successful with { Description = new string('W',255), CustomerName = new string('M',100) },user!,DateTime.Now);
        File.WriteAllBytes("output/verification/receipt-long.pdf",ReceiptExporter.CreatePdf(longReceipt));
        await UserSession.SignOutAsync();
        Check(UserSession.Current == null && app.MainWindow is LoginAppGUI,"Logout clears session and returns to login");
        try { UserSession.RequireUser(); throw new Exception("Session remains accessible"); }
        catch (InvalidOperationException) { Check(true,"Feature access rejected after logout"); }
        await using var db = await DBConnectandManage.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.ActivityLog WHERE accountId=900001 AND action=N'Đăng xuất'",db);
        Check(Convert.ToInt32(await cmd.ExecuteScalarAsync())>0,"Logout written to ActivityLog");
    }
}
