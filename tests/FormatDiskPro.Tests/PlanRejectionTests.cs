using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El rechazo de un plan de particiones tiene que decir <b>qué cambiar</b>, no solo que no vale
/// (`T13-18`). Estas pruebas recorren el enum entero: añadir un <see cref="PlanProblem"/> obliga a
/// decidir qué se le dice a quien lo provoque, igual que añadir un color obliga a medirlo.
/// </summary>
public class PlanRejectionTests
{
    private static PartitionPlan WholeDiskFat32 =>
        PartitionPlan.WholeDisk(DiskPartitionStyle.Mbr, "FAT32", "DATOS");

    /// <summary>Todos los motivos menos <c>None</c>: lo que se puede rechazar de verdad.</summary>
    public static TheoryData<PlanProblem> EveryProblem()
    {
        var data = new TheoryData<PlanProblem>();
        foreach (PlanProblem p in Enum.GetValues<PlanProblem>())
            if (p != PlanProblem.None) data.Add(p);
        return data;
    }

    public static TheoryData<PlanProblem> ProblemsFromTheForm()
    {
        var data = new TheoryData<PlanProblem>();
        foreach (PlanProblem p in PlanRejection.FromTheForm) data.Add(p);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryProblem))]
    public void EveryProblem_HasATextInTheFiveLanguages(PlanProblem problem)
    {
        var (key, args) = PlanRejection.Describe(new PlanValidation(problem, 0), WholeDiskFat32, 60_000_000_000);

        var prev = L.Current;
        try
        {
            foreach (AppLang lang in Enum.GetValues<AppLang>())
            {
                L.Set(lang);
                string text = L.T(key, args);

                Assert.False(string.IsNullOrWhiteSpace(text), $"{problem} ({lang}) se queda sin texto.");
                // Si la clave no existiera, L.T devolvería la propia clave: un texto vacío de significado.
                Assert.NotEqual(key, text);
                // Un {0} sin sustituir significa que Describe no pasó los argumentos que el texto pide.
                Assert.DoesNotContain("{0}", text);
                Assert.DoesNotContain("{1}", text);
            }
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// Los motivos que el formulario sí puede provocar no pueden quedarse en el texto genérico: ese dice
    /// que el plan no vale y calla la salida, que es justo el defecto que abrió `T13-18`.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProblemsFromTheForm))]
    public void WhatTheFormCanCause_SaysWhatToChange(PlanProblem problem)
    {
        var (key, _) = PlanRejection.Describe(new PlanValidation(problem, 0), WholeDiskFat32, 60_000_000_000);

        Assert.NotEqual(PlanRejection.GenericKey, key);
    }

    /// <summary>
    /// El caso que destapó todo esto, de punta a punta: el disco de 59,8 GB entero en FAT32 que sugiere el
    /// selector cuando la unidad elegida es un volumen de 28,5 GB. El mensaje tiene que nombrar el límite,
    /// el tamaño que se iba a crear y las dos salidas.
    /// </summary>
    [Fact]
    public void TheRealCase_NamesTheLimit_TheSizeAndTheWayOut()
    {
        const long disk = 59_797_659_648; // la USB de pruebas
        var plan = WholeDiskFat32;
        var validation = plan.Validate(disk);

        Assert.Equal(PlanProblem.Fat32VolumeTooLarge, validation.Problem);

        // El idioma se fija ANTES de Describe, no solo antes de traducir: los tamaños los formatea
        // FormatBytes con la cultura activa, así que con otro idioma puesto saldría «55.7 GB» con punto y
        // esta prueba fallaría según el orden en que xUnit ejecutara las clases (visto).
        var prev = L.Current;
        string text;
        try
        {
            L.Set(AppLang.Es);
            var (key, args) = PlanRejection.Describe(validation, plan, disk);
            text = L.T(key, args);
        }
        finally { L.Set(prev); }

        Assert.Contains("32 GB", text);      // el límite
        Assert.Contains("55,7 GB", text);    // lo que se iba a crear (el disco entero)
        Assert.Contains("exFAT", text);      // la salida
        Assert.Contains("FAT32 pequeña", text);
    }

    /// <summary>
    /// El tamaño que se nombra sale del volumen culpable, no del disco. Con la FAT32 pequeña marcada, el
    /// que se pasa de la raya es el <b>sobrante</b>, y es su tamaño el que hay que decir.
    /// </summary>
    [Fact]
    public void TheSizeItNames_IsTheOffendingVolume_NotTheDisk()
    {
        const long disk = 59_797_659_648;
        var plan = new PartitionPlan(DiskPartitionStyle.Mbr,
        [
            new PartitionSpec(new PartitionSize.Exact(1_073_741_824), "FAT32", "BIOS"),
            new PartitionSpec(new PartitionSize.Remainder(), "FAT32", "RESTO"),
        ]);

        var validation = plan.Validate(disk);
        Assert.Equal(PlanProblem.Fat32VolumeTooLarge, validation.Problem);
        Assert.Equal(1, validation.PartitionIndex);

        var (_, args) = PlanRejection.Describe(validation, plan, disk);
        Assert.Equal(FormatLogic.FormatBytes(plan.EffectiveSizes(disk)[1]), args[1]);
    }
}
