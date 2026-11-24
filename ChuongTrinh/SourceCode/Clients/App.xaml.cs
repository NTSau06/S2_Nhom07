using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace BankingClientApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var vi = new CultureInfo("vi-VN");
            CultureInfo.DefaultThreadCurrentCulture = vi;
            CultureInfo.DefaultThreadCurrentUICulture = vi;
            CultureInfo.CurrentCulture = vi;
            CultureInfo.CurrentUICulture = vi;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(vi.IetfLanguageTag)));
            base.OnStartup(e);
        }
    }
}
