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
    /// <summary>Glifo Segoe Fluent Icons según el tipo de unidad (USB / RAM / disco fijo).</summary>
    public string Glyph { get; }

    public SolidColorBrush ForegroundBrush
    {
        get => _foregroundBrush;
        set { _foregroundBrush = value; PropertyChanged?.Invoke(this, new(nameof(ForegroundBrush))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// El texto que se ve, que es también <b>cómo se anuncia</b> la unidad (`AUD-003`).
    /// </summary>
    /// <remarks>
    /// Un ítem de <c>ComboBox</c> enlazado a un objeto toma su nombre accesible de <c>ToString()</c>, no
    /// de la plantilla que lo pinta. Sin esta línea todas las unidades se anunciaban igual —con el
    /// nombre del tipo, «FormatDiskPro.UI.DriveViewModel»—: un lector de pantalla no distinguía el
    /// pendrive del disco de Windows en el selector de lo que se va a formatear.
    /// </remarks>
    public override string ToString() => DisplayText;

    public DriveViewModel(char letter, string label, DriveInfo info, bool isProtected, SolidColorBrush brush)
    {
        Letter = letter;
        // protected.tag ya incluye corchetes y espacio (p. ej. "[Protegido] "): no envolver de nuevo.
        DisplayText = isProtected ? $"{L.T("protected.tag")}{label}" : label;
        Info = info;
        IsProtected = isProtected;
        _foregroundBrush = brush;
        Glyph = info.DriveType switch
        {
            DriveType.Removable => "\uE88E",   // USB
            DriveType.Ram       => "\uE950",   // Component (chip de memoria)
            _                   => "\uEDA2",   // HardDrive
        };
    }
}
