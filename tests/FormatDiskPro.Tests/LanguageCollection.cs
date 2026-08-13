using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Colección para las clases que cambian el idioma activo. <see cref="FormatDiskPro.L.Current"/> es
/// estado estático global del proceso: dos clases moviéndolo a la vez (xUnit paraleliza entre
/// colecciones por defecto) se pisarían y producirían fallos intermitentes. Marcar ambas con esta
/// colección las serializa entre sí, sin frenar al resto de la suite.
/// </summary>
[CollectionDefinition(Name)]
public sealed class LanguageCollection
{
    public const string Name = "Idioma activo";
}
