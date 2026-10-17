// SPDX-License-Identifier: GPL-3.0-only
using System.Runtime.InteropServices;

namespace FormatDiskPro;

/// <summary>
/// Lo que necesita la E/S <b>sin caché</b> del sistema, la que usan el benchmark y la lectura de la
/// verificación de capacidad para medir y leer el disco y no la memoria.
/// </summary>
/// <remarks>
/// Una sola definición (`AUD-041`): la opción, la alineación y el búfer alineado estaban copiados en
/// <see cref="BenchmarkRunner"/> y <see cref="CapacityVerifier"/>.
/// </remarks>
internal static class UnbufferedIo
{
    /// <summary><c>FILE_FLAG_NO_BUFFERING</c>, que <see cref="FileOptions"/> no expone; el valor es el de la API Win32.</summary>
    public const FileOptions NoBuffering = (FileOptions)0x2000_0000;

    /// <summary>Alineación de sector que exige la E/S sin caché: de la memoria, del desplazamiento y del tamaño.</summary>
    public const int Alignment = 4096;
}

/// <summary>
/// Bloques de memoria alineados a <see cref="UnbufferedIo.Alignment"/>, sobre un único array fijado.
/// </summary>
/// <remarks>
/// El montón de objetos fijados (POH) no garantiza la alineación, así que se pide de más, se fija el array con
/// un <see cref="GCHandle"/> y se toman los sub-rangos que empiezan en una dirección alineada. Hay que
/// liberarlo (<see cref="Dispose"/>) cuando ya no haya E/S en vuelo sobre él.
/// </remarks>
internal sealed class AlignedBuffers : IDisposable
{
    private readonly byte[] _raw;
    private GCHandle _pin;

    /// <summary>Reserva <paramref name="count"/> bloques de <paramref name="blockSize"/> bytes.</summary>
    /// <param name="count">El número de bloques.</param>
    /// <param name="blockSize">El tamaño de cada bloque, múltiplo de <see cref="UnbufferedIo.Alignment"/>.</param>
    public AlignedBuffers(int count, int blockSize)
    {
        const int a = UnbufferedIo.Alignment;
        _raw = new byte[count * blockSize + a];
        _pin = GCHandle.Alloc(_raw, GCHandleType.Pinned);
        long addr = _pin.AddrOfPinnedObject().ToInt64();
        int pad = (int)((a - (addr & (a - 1))) & (a - 1));
        Blocks = new Memory<byte>[count];
        for (int i = 0; i < count; i++)
            Blocks[i] = _raw.AsMemory(pad + i * blockSize, blockSize);
    }

    /// <summary>Obtiene los bloques, cada uno alineado y del tamaño pedido.</summary>
    public Memory<byte>[] Blocks { get; }

    /// <summary>Suelta el array fijado.</summary>
    public void Dispose()
    {
        if (_pin.IsAllocated) _pin.Free();
    }
}
