using System.Windows;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.ToolWindows.Chat;

public partial class ApprovalDialog : Window
{
    private readonly Services.RiskLevel _riskLevel;

    public ApprovalResult? Result { get; private set; }

    public ApprovalDialog(string actionDescription, string details, Services.RiskLevel riskLevel)
    {
        InitializeComponent();
        _riskLevel = riskLevel;

        ActionText.Text = actionDescription;
        DetailsBox.Text = details;

        RiskLevelText.Text = riskLevel switch
        {
            RiskLevel.Low => "\u26A0 Low Risk",
            RiskLevel.Medium => "\u26A0 Medium Risk",
            RiskLevel.High => "\u26A0 High Risk",
            RiskLevel.Critical => "\u26A0 Critical Risk",
            _ => "\u26A0 Unknown Risk"
        };

        RiskLevelText.Foreground = riskLevel switch
        {
            RiskLevel.Low => System.Windows.Media.Brushes.Green,
            RiskLevel.Medium => System.Windows.Media.Brushes.Orange,
            RiskLevel.High or RiskLevel.Critical => System.Windows.Media.Brushes.Red,
            _ => System.Windows.Media.Brushes.Gray
        };
    }

    private void OnApprove(object sender, RoutedEventArgs e)
    {
        Result = new ApprovalResult(true, CommentBox.Text, null);
        DialogResult = true;
    }

    private void OnReject(object sender, RoutedEventArgs e)
    {
        Result = new ApprovalResult(false, CommentBox.Text, null);
        DialogResult = true;
    }
}
