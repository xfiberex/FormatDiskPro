using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// <c>[Fact]</c> que se <b>OMITE</b> salvo que se indique una unidad real sobre la que probar, en
/// <c>FORMATDISKPRO_VERIFY_DRIVE</c> (p. ej. <c>D</c>).
///
/// Las unitarias corren en cualquier máquina y no deben depender de que haya una USB conectada; pero la
/// verificación de capacidad hace E/S <b>sin caché del sistema</b>, y esa ruta impone alineación de sector:
/// el disco local del desarrollador no es prueba suficiente de que funcione sobre el medio extraíble que
/// esta función existe para examinar. Con la variable puesta, la prueba lo comprueba de verdad.
/// </summary>
public sealed class VerifyDriveFactAttribute : FactAttribute
{
    public const string DriveVar = "FORMATDISKPRO_VERIFY_DRIVE";

    public VerifyDriveFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DriveVar)))
            Skip = $"Requiere una unidad real donde escribir: define {DriveVar}=<letra> (p. ej. D) " +
                   "antes de 'dotnet test'. Escribe y borra ~64 MB en ella; no formatea nada.";
    }
}

/// <summary>
/// La verificación de capacidad, ejercitada sobre una <b>unidad real</b> (normalmente la USB de pruebas).
///
/// <para>Lo que aporta sobre <see cref="CapacityVerifierTests"/>, que corre en el disco local: la relectura
/// sin caché (`T2-03`) exige que buffer, desplazamiento y longitud estén alineados al <b>sector del medio
/// concreto</b>. Un disco fijo y una USB no tienen por qué anunciar la misma geometría, y el modo sin
/// caché falla con «El parámetro no es correcto» en cuanto algo no cuadra — no degrada, falla.</para>
/// </summary>
public sealed class CapacityVerifierDriveTests
{
    private static readonly Progress<(CapacityVerifier.Phase, int, long)> Ignore = new();

    [VerifyDriveFact]
    public async Task RunInAsync_OnRealDrive_VerifiesWithoutTheSystemCache()
    {
        string letter = Environment.GetEnvironmentVariable(VerifyDriveFactAttribute.DriveVar)!.Trim();
        var drive = new DriveInfo(letter);
        Assert.True(drive.IsReady, $"La unidad {letter}: no está lista.");

        // 64 MB: suficiente para varios bloques de 8 MB (y para que el patrón anti-aliasing tenga sentido),
        // lo bastante poco para no tardar. El directorio se borra solo, pase lo que pase.
        string dir = Path.Combine(drive.RootDirectory.FullName, $"__fdp_verify_test_{Guid.NewGuid():N}");
        long target = 64L * 1024 * 1024;

        var result = await CapacityVerifier.RunInAsync(dir, target, Ignore, CancellationToken.None);

        Assert.True(result.Ok,
            $"La unidad {letter}: (buena, por hipótesis) no verificó: '{result.FailureDetail}'. " +
            "Si el detalle es una excepción de parámetro, es la alineación de la E/S sin caché.");
        Assert.Equal(target, result.WrittenBytes);
        Assert.False(Directory.Exists(dir), "El directorio de trabajo debe borrarse al terminar.");
    }
}
