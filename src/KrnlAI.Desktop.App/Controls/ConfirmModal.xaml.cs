using System.Windows;

namespace KrnlAI.Desktop.App.Controls;

public partial class ConfirmModal : Window
{
    public bool Confirmed { get; private set; }

    private ConfirmModal()
    {
        InitializeComponent();
        ConfirmButton.Click += (s, e) => { Confirmed = true; Close(); };
        CancelButton.Click += (s, e) => { Confirmed = false; Close(); };
    }

    public static bool Show(Window owner, string title, string message, bool danger = false)
    {
        var dialog = new ConfirmModal
        {
            Owner = owner,
            Title = title
        };
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;

        if (danger)
        {
            dialog.ConfirmButton.Style = (System.Windows.Style)Application.Current.Resources["DangerButton"];
        }

        dialog.ShowDialog();
        return dialog.Confirmed;
    }
}
