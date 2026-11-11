// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro.UI;

/// <summary>
/// Guía rápida (<em>Ayuda → Guía rápida…</em>, F1): formatear en tres pasos, qué sistema de archivos elegir,
/// qué herramientas no tocan los datos, qué hacer si algo falla y los atajos (`AUD-077`).
/// </summary>
/// <remarks>
/// Los nombres de menús y botones que cita se toman de sus propias claves, no se escriben en el texto: así no
/// pueden remitir a un menú que se llame de otra forma (la lección de `AUD-045`).
/// </remarks>
public sealed partial class GuideDialog : ContentDialog
{
    /// <param name="updates">Servicio que abre el README en el navegador.</param>
    public GuideDialog(IUpdateService updates)
    {
        InitializeComponent();

        Title                = L.T("guide.title");
        SecondaryButtonText  = L.T("guide.more");
        CloseButtonText      = L.T("btn.close");
        DefaultButton        = ContentDialogButton.None;

        string tools = L.T("menu.tools");
        IntroText.Text       = L.T("guide.intro");
        FormatHeading.Text   = L.T("guide.formatTitle");
        SetBody(FormatBody, L.T("guide.formatBody", L.T("fmt.presets"), L.T("btn.start.drive", "X:")));
        FsHeading.Text       = L.T("guide.fsTitle");
        SetBody(FsBody, L.T("guide.fsBody"));
        SafeHeading.Text     = L.T("guide.safeTitle");
        SetBody(SafeBody, L.T("guide.safeBody",
                                   MenuName("menu.health"), MenuName("menu.check"),
                                   MenuName("menu.benchmark"), MenuName("menu.verify")));
        TroubleHeading.Text  = L.T("guide.troubleTitle");
        SetBody(TroubleBody, L.T("guide.troubleBody",
                                   $"{tools} → {MenuName("menu.unlock")}",
                                   $"{tools} → {MenuName("menu.reinit")}",
                                   L.T("bar.history")));
        KeysHeading.Text     = L.T("guide.keysTitle");
        KeysNote.Text        = L.T("guide.keysNote");

        AddKeys(
        [
            ("F1",     MenuName("menu.guide")),
            ("F5",     L.T("tip.refresh")),
            ("Ctrl+I", MenuName("menu.health")),
            ("Ctrl+B", MenuName("menu.benchmark")),
            ("Ctrl+H", MenuName("menu.history")),
            ("Ctrl+E", L.T("guide.keyExport")),
        ]);

        // Abrir el README sin cerrar la guía; si el navegador no abre, se enseña la dirección.
        SecondaryButtonClick += (_, args) =>
        {
            args.Cancel = true;
            LinkBar.IsOpen = false;
            if (updates.OpenUrl(AppInfo.GuideUrl)) return;
            LinkBar.Message = L.T("link.failed", AppInfo.GuideUrl);
            LinkBar.IsOpen  = true;
        };
    }

    /// <summary>El texto de un ítem del menú sin los puntos suspensivos, para citarlo dentro de una frase.</summary>
    private static string MenuName(string key) => L.T(key).TrimEnd('…', ' ');

    /// <summary>
    /// Pinta un cuerpo de la guía línea a línea. Las que empiezan por «• » o por «1. » llevan el marcador en su
    /// propia columna, así el texto que no cabe en un renglón sigue alineado con el de arriba.
    /// </summary>
    private void SetBody(StackPanel host, string text)
    {
        var body = (Style)Resources["GuideBodyStyle"];
        foreach (string line in text.Split('\n'))
        {
            var m = System.Text.RegularExpressions.Regex.Match(line, @"^(•|\d+\.)\s+(.*)$");
            if (!m.Success)
            {
                host.Children.Add(new TextBlock { Text = line, Style = body });
                continue;
            }
            var row = new Grid { ColumnSpacing = 6 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var marker = new TextBlock { Text = m.Groups[1].Value, Style = body };
            var content = new TextBlock { Text = m.Groups[2].Value, Style = body };
            Grid.SetColumn(content, 1);
            row.Children.Add(marker);
            row.Children.Add(content);
            host.Children.Add(row);
        }
    }

    private void AddKeys(IReadOnlyList<(string Key, string Label)> keys)
    {
        var keyStyle   = (Style)Resources["GuideKeyStyle"];
        var labelStyle = (Style)Resources["GuideKeyLabelStyle"];
        for (int row = 0; row < keys.Count; row++)
        {
            KeysGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var key   = new TextBlock { Text = keys[row].Key, Style = keyStyle };
            var label = new TextBlock { Text = keys[row].Label, Style = labelStyle };
            Grid.SetRow(key, row);
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 1);
            KeysGrid.Children.Add(key);
            KeysGrid.Children.Add(label);
        }
    }
}
