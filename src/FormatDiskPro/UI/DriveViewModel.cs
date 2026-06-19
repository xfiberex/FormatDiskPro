using Microsoft.UI.Xaml.Media;
using System.ComponentModel;

namespace FormatDiskPro.UI;

public sealed class DriveViewModel : INotifyPropertyChanged
{
    private SolidColorBrush _foregroundBrush;

    public char Letter { get; }
    public string DisplayText { get; }
    public DriveInfo Info { get; }
    public bool IsProtected { get; }

    public SolidColorBrush ForegroundBrush
    {
        get => _foregroundBrush;
        set { _foregroundBrush = value; PropertyChanged?.Invoke(this, new(nameof(ForegroundBrush))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DriveViewModel(char letter, string label, DriveInfo info, bool isProtected, SolidColorBrush brush)
    {
        Letter = letter;
        DisplayText = isProtected ? $"[{L.T("protected.tag")}]{label}" : label;
        Info = info;
        IsProtected = isProtected;
        _foregroundBrush = brush;
    }
}
