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
    private static void Main(string[] args)
    {
        if (args.Contains("--analysis-only"))
        {
            try { TestAnalysis(); }
            catch (Exception ex) { results.Add("FAIL: " + ex); Environment.ExitCode = 1; }
            foreach (var line in results) Console.WriteLine(line);
            return;
        }
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
    private static void TestAnalysis()
    {
        var rows = new List<TransactionBank>
        {
            new("1", "A", "Khách A", 2500000m, "Chuyển khoản", "Thành công", DateTime.Today, "", false, ""),
            new("2", "B", "Khách B", 10000000m, "Nộp tiền", "Thành công", DateTime.Today, "", false, ""),
            new("3", "A", "Khách A", 500000m, "Rút tiền", "Đang xử lý", DateTime.Today, "", false, ""),
            new("4", "B", "Khách B", 20000000m, "Chuyển khoản", "Thất bại", DateTime.Today, "", true, "")
        };
        var total = TransactionAnalysis.Summarize("Tất cả", rows);
        Check(total.Count == 4 && total.TotalAmount == 33000000m && total.SuccessfulCount == 2 && total.FlaggedCount == 1,
            "Statistics count each transaction once and include all statuses in total amount");
        Check(TransactionAnalysis.Summarize("Thành công", rows.Where(t => t.CanIssueReceipt)).TotalAmount == 12500000m,
            "Successful amount excludes pending and failed transactions");
        var filtered = TransactionAnalysis.Filter(rows, "Chuyển khoản", 2500000m, 20000000m, false);
        Check(filtered.Count == 1 && filtered[0].Id == "1", "Combine type, inclusive amount bounds and flag filters");
        Check(TransactionAnalysis.Filter(rows, "", 2500000m, 2500000m, null).Single().Id == "1", "Equal amount bounds include exact amount");
        Check(TransactionAnalysis.Filter(rows, "", null, null, true).Single().Id == "4", "Flagged-only filter");
        Check(TransactionAnalysis.Filter(rows, "", null, null, null).Count == 4, "Cleared extra filters include all transactions");
        Check(TransactionAnalysis.Summarize("Lọc", filtered).TotalAmount == 2500000m, "Statistics use only filtered rows");
        var byType = TransactionAnalysis.GroupBy(rows, t => t.TransactionType);
        Check(byType.Count == 3 && byType.Single(g => g.Group == "Chuyển khoản").TotalAmount == 22500000m,
            "Group statistics by transaction type");
        Check(TransactionAnalysis.GroupBy(rows, t => t.Status).Count == 3, "Group statistics by transaction status");
        var empty = TransactionAnalysis.Filter(rows, "Không tồn tại", null, null, null);
        var emptyStats = TransactionAnalysis.Summarize("Trống", empty);
        Check(emptyStats.Count == 0 && emptyStats.TotalAmount == 0 && emptyStats.SuccessRateText == "0,0%",
            "Empty results reset statistics without division by zero");
        Check(TransactionAnalysis.Summarize("Alias", new[] { rows[0] with { Status = "Completed" } }).SuccessfulCount == 1,
            "Statistics share receipt success status rules");
        try { TransactionAnalysis.Filter(rows, "", 20m, 10m, null); throw new Exception("Invalid range accepted"); }
        catch (ArgumentException) { Check(true, "Reject reversed amount range"); }
        try { TransactionAnalysis.Filter(rows, "", -1m, null, null); throw new Exception("Negative amount accepted"); }
        catch (ArgumentException) { Check(true, "Reject negative amount range"); }
    }
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
        TestAnalysis();
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
        var keyword = (System.Windows.Controls.TextBox)main.FindName("KeywordBox");
        var search = (System.Windows.Controls.Button)main.FindName("SearchButton");
        async Task ApplyFilters()
        {
            search.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            for (int i = 0; i < 100 && !search.IsEnabled; i++) await Task.Delay(100);
            Check(search.IsEnabled, "Filter request finishes");
        }
        keyword.Text = "DEMO-";
        await ApplyFilters();
        Check(grid.Items.Count == 4 && ((System.Windows.Controls.TextBlock)main.FindName("SummaryText")).Text.Contains("50,0%"),
            "UI summary reflects all four demo transactions");
        ((System.Windows.Controls.ComboBox)main.FindName("TypeBox")).Text = "Chuyển khoản";
        ((System.Windows.Controls.TextBox)main.FindName("MinimumAmountBox")).Text = "2500000";
        ((System.Windows.Controls.TextBox)main.FindName("MaximumAmountBox")).Text = "20000000";
        ((System.Windows.Controls.ComboBox)main.FindName("FlagBox")).SelectedIndex = 2;
        await ApplyFilters();
        Check(grid.Items.Count == 1 && ((TransactionBank)grid.Items[0]).Id == "DEMO-GD001", "UI combines advanced filters");
        var grouped = (System.Windows.Controls.DataGrid)main.FindName("TypeStatisticsGrid");
        Check(grouped.Items.Count == 1 && ((TransactionStatistics)grouped.Items[0]).TotalAmount == 2500000m,
            "UI grouped statistics match filtered transaction amount");
        ((System.Windows.Controls.TextBox)main.FindName("MinimumAmountBox")).Text = "abc";
        await ApplyFilters();
        Check(((System.Windows.Controls.TextBlock)main.FindName("MessageText")).Text.Contains("Số tiền phải") && grid.Items.Count == 1,
            "Invalid amount shows validation and preserves applied results");
        ((System.Windows.Controls.Button)main.FindName("ResetButton")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        for (int i = 0; i < 100 && !search.IsEnabled; i++) await Task.Delay(100);
        Check(grid.Items.Count >= 4 && keyword.Text == "" && ((System.Windows.Controls.ComboBox)main.FindName("TypeBox")).Text == "",
            "Reset clears advanced filters and restores results");
        keyword.Text = "no-match";
        await ApplyFilters();
        Check(grid.Items.Count == 0 && grouped.Items.Count == 0 && ((System.Windows.Controls.TextBlock)main.FindName("SummaryText")).Text.Contains("0,0%"),
            "Empty UI search clears grouped statistics and percentage");
        keyword.Text = "DEMO-";
        await ApplyFilters();
        ((System.Windows.Controls.DataGrid)main.FindName("TransactionsGrid")).SelectedItem = transactions.Single(t=>t.Id=="DEMO-GD001");
        await Task.Delay(200);
        Capture(main,"02-transactions");
        var statisticsTab = (System.Windows.Controls.TabItem)main.FindName("StatisticsTab");
        statisticsTab.IsSelected = true;
        await Task.Delay(100);
        Capture(main,"05-statistics");
        statisticsTab.IsSelected = false;
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
