namespace FormatDiskPro;

/// <summary>
/// Cómo queda en el historial un formato que terminó bien (`AUD-009`).
/// </summary>
/// <remarks>
/// Vivía como una cadena interpolada en <c>MainWindow.RunFormatAsync</c>, y escribía <c>wipe=True</c>
/// con que el borrado seguro se hubiera <b>pedido</b>. El servicio puede no ejecutarlo —devuelve un
/// código distinto de cero si la unidad no estaba lista— y nadie miraba ese código: el historial, que es
/// el registro de auditoría de la app, afirmaba un borrado que no había ocurrido.
/// </remarks>
public static class FormatHistory
{
    /// <summary>Código con el que el borrado seguro dice que se completó.</summary>
    public const int WipeCompleted = 0;

    /// <summary>¿Se hizo de verdad el borrado seguro?</summary>
    /// <param name="requested">Si se pidió.</param>
    /// <param name="wipeCode">Lo que devolvió el servicio; se ignora si no se pidió.</param>
    public static bool WipeDone(bool requested, int wipeCode) => requested && wipeCode == WipeCompleted;

    /// <summary>
    /// Línea <c>FORMAT OK</c>. <c>wipe=True</c> —y el número de pasadas— solo si el borrado se
    /// <b>completó</b>, no si solo se pidió.
    /// </summary>
    public static string Success(
        char letter, string fs, long allocBytes, bool quick, bool compress,
        bool wipeRequested, int wipeCode, int passes, string label)
    {
        bool wiped = WipeDone(wipeRequested, wipeCode);
        return $"FORMAT OK {letter}: fs={fs} alloc={allocBytes} quick={quick} compress={compress} " +
               $"wipe={wiped}{(wiped ? $" passes={passes}" : "")} label='{label}'";
    }

    /// <summary>Línea para un borrado seguro que se pidió y no se ejecutó.</summary>
    public static string WipeFailed(char letter, int wipeCode) => $"WIPE FAIL {letter}: code={wipeCode}";
}
