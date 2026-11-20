using System.Globalization;
using System.Windows;
using System.Windows.Markup;
namespace BankingManagesandServicesApp;
public partial class App : Application
{
    static App()
    {
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("vi-VN")));
    }
}
