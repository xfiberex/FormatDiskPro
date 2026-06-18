namespace FormatDiskPro;

/// <summary>
/// Diálogo de confirmación reforzada: el usuario debe escribir la letra de la unidad
/// para habilitar el botón de aceptar. Capa anti-accidente para una operación destructiva.
/// </summary>
public sealed class ConfirmFormatDialog : Form
{
    private readonly TextBox _input = new();
    private readonly Button  _ok    = new();

    public ConfirmFormatDialog(char driveLetter, string summary)
    {
        string letter = char.ToUpper(driveLetter).ToString();

        Text            = L.T("confirm.title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        ClientSize      = new Size(430, 280);
        Font            = new Font("Segoe UI", 9F);

        var icon = new PictureBox
        {
            Image    = SystemIcons.Warning.ToBitmap(),
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new Point(16, 16),
        };

        var lblSummary = new Label
        {
            Text     = summary,
            Location = new Point(64, 16),
            Size     = new Size(350, 150),
        };

        var lblPrompt = new Label
        {
            Text      = L.T("confirm.prompt", letter),
            Location  = new Point(16, 178),
            Size      = new Size(398, 24),
            ForeColor = Color.FromArgb(192, 0, 0),
        };

        _input.Location     = new Point(16, 204);
        _input.Size         = new Size(398, 27);
        _input.CharacterCasing = CharacterCasing.Upper;
        _input.MaxLength    = 1;
        _input.TextChanged += (_, _) => _ok.Enabled = _input.Text.Trim() == letter;

        _ok.Text         = L.T("btn.start");
        _ok.DialogResult = DialogResult.OK;
        _ok.Location     = new Point(218, 240);
        _ok.Size         = new Size(95, 32);
        _ok.Enabled      = false;

        var cancel = new Button
        {
            Text         = L.T("btn.cancel"),
            DialogResult = DialogResult.Cancel,
            Location     = new Point(319, 240),
            Size         = new Size(95, 32),
        };

        Controls.AddRange([icon, lblSummary, lblPrompt, _input, _ok, cancel]);
        AcceptButton = _ok;
        CancelButton = cancel;
    }
}
