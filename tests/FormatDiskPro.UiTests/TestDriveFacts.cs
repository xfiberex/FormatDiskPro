namespace FormatDiskPro.UiTests;

/// <summary>
/// <c>[Fact]</c> que se <b>SALTA</b> (no falla) si la USB de pruebas no está conectada.
///
/// Antes, estos tests llamaban a <see cref="TestDrive.RequireLetter"/>, que lanza: sin la USB conectada
/// daban <b>rojo</b>. Eso confunde dos cosas muy distintas —"la app está rota" y "no tengo el hardware
/// delante"— y hacía imposible meter los UI tests en el pipeline de release: cualquier corte hecho sin la
/// USB pinchada habría abortado por seis fallos que no eran fallos.
///
/// Con esto, un corte sin la USB los marca como <b>omitidos</b> (visibles en el resumen de
/// <c>dotnet test</c>, y <c>release.ps1 -UiTests</c> avisa de cuántos se saltaron) y los demás siguen
/// ejerciendo la app real. Si la USB SÍ está y algo se rompe, el test falla como debe.
///
/// La comprobación ocurre al <b>descubrir</b> los tests, no al ejecutarlos: si la unidad se desconecta a
/// media corrida, <see cref="TestDrive.RequireLetter"/> dentro del test sigue lanzando, que es lo correcto
/// (ahí sí es un fallo real).
/// </summary>
public sealed class TestDriveFactAttribute : FactAttribute
{
    public TestDriveFactAttribute()
    {
        if (TestDrive.FindLetter(TestDrive.PrimaryLabel) is null)
            Skip = $"Requiere la USB de pruebas conectada (partición extraíble etiquetada " +
                   $"'{TestDrive.PrimaryLabel}').";
    }
}

/// <summary>
/// <c>[Fact]</c> de una prueba que <b>BORRA DATOS REALES</b> en la USB de pruebas. Se salta salvo que se
/// pida explícitamente con <c>FORMATDISKPRO_ALLOW_DESTRUCTIVE=1</c> <b>y</b> la unidad esté conectada.
///
/// El opt-in ya existía (<see cref="TestDrive.RequireDestructiveOptIn"/>), pero <b>lanzando</b>: sin la
/// variable, la prueba salía en rojo. Saltarla es lo correcto —no ejecutarla es el comportamiento
/// deseado por defecto, no un fallo— y además es lo que permite que <c>release.ps1 -UiTests</c> corra la
/// suite sin riesgo: un corte de release <b>nunca</b> debe formatear una unidad.
/// </summary>
public sealed class DestructiveFactAttribute : FactAttribute
{
    public DestructiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(TestDrive.DestructiveOptInVar) != "1")
            Skip = $"Prueba destructiva (formatea/reinicializa de verdad la USB de pruebas). Define " +
                   $"{TestDrive.DestructiveOptInVar}=1 antes de 'dotnet test' para ejecutarla.";
        else if (TestDrive.FindLetter(TestDrive.PrimaryLabel) is null)
            Skip = $"Requiere la USB de pruebas conectada (partición extraíble etiquetada " +
                   $"'{TestDrive.PrimaryLabel}').";
    }
}
