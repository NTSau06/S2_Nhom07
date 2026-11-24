using System.Windows;
using BankingManagesandServicesApp.Services;
namespace BankingManagesandServicesApp.GUIs;

// All feature windows inherit this guard and include SessionHeader.
public class AuthenticatedWindow : Window
{
    public AuthenticatedWindow() => SetResourceReference(StyleProperty, typeof(Window));
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (UserSession.Current == null) Close();
    }
}
