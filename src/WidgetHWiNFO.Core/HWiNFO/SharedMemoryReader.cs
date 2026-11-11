using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

namespace WidgetHWiNFO.Core.HWiNFO;

/// <summary>
/// Reads HWiNFO's shared memory. The mapping is opened and closed on every read on purpose:
/// holding it open would keep a stale mapping alive after HWiNFO exits and could interfere
/// with HWiNFO re-creating it when restarted.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SharedMemoryReader
{
    public const string MapName = @"Global\HWiNFO_SENS_SM2";

    private byte[] _buffer = new byte[64 * 1024];

    public HwInfoStatus TryRead(out HwSnapshot? snapshot)
    {
        snapshot = null;
        try
        {
            using var mmf = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.Read);

            Span<byte> headerBytes = stackalloc byte[HwInfoParser.HeaderSizeWithPeriod];
            using (var hv = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
            {
                if (hv.ReadAtLeast(headerBytes, HwInfoParser.HeaderSize, throwOnEndOfStream: false) < HwInfoParser.HeaderSize)
                    return HwInfoStatus.Invalid;
            }

            if (!HwInfoParser.TryParseHeader(headerBytes, out var header)) return HwInfoStatus.Invalid;
            if (header.Signature == HwInfoParser.SignatureInactive) return HwInfoStatus.Inactive;
            if (header.Signature != HwInfoParser.SignatureActive || !HwInfoParser.IsStructurallyValid(header))
                return HwInfoStatus.Invalid;

            long total = header.TotalSize;
            if (total > 64L * 1024 * 1024) return HwInfoStatus.Invalid;
            if (_buffer.Length < total) _buffer = new byte[total];

            using (var view = mmf.CreateViewAccessor(0, total, MemoryMappedFileAccess.Read))
            {
                view.ReadArray(0, _buffer, 0, (int)total);
            }

            return HwInfoParser.TryParse(_buffer.AsSpan(0, (int)total), out snapshot);
        }
        catch (FileNotFoundException)
        {
            return HwInfoStatus.NotRunning;
        }
        catch (UnauthorizedAccessException)
        {
            return HwInfoStatus.NotRunning;
        }
        catch (IOException)
        {
            return HwInfoStatus.NotRunning;
        }
    }
}
