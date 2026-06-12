using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Segaris.Api.Platform.Attachments;

internal static class AttachmentPolicy
{
    public const long MaximumFileSize = 25L * 1024 * 1024;
    public const int MaximumFileNameLength = 255;

    private static readonly IReadOnlyDictionary<string, FileRule> Rules =
        new Dictionary<string, FileRule>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new("application/pdf", ValidatePdf),
            [".jpg"] = new("image/jpeg", ValidateJpeg),
            [".jpeg"] = new("image/jpeg", ValidateJpeg),
            [".png"] = new("image/png", ValidatePng),
            [".webp"] = new("image/webp", ValidateWebp),
            [".txt"] = new("text/plain", ValidateText),
            [".csv"] = new("text/csv", ValidateText),
            [".md"] = new("text/markdown", ValidateText),
            [".json"] = new("application/json", ValidateJson),
            [".xml"] = new("application/xml", ValidateXml),
            [".yaml"] = new("application/yaml", ValidateText),
            [".yml"] = new("application/yaml", ValidateText),
            [".docx"] = new(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                stream => ValidateZipPackage(stream, "word/document.xml")),
            [".xlsx"] = new(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                stream => ValidateZipPackage(stream, "xl/workbook.xml")),
            [".pptx"] = new(
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                stream => ValidateZipPackage(stream, "ppt/presentation.xml")),
            [".odt"] = new(
                "application/vnd.oasis.opendocument.text",
                stream => ValidateZipPackage(stream, "content.xml")),
            [".ods"] = new(
                "application/vnd.oasis.opendocument.spreadsheet",
                stream => ValidateZipPackage(stream, "content.xml")),
            [".odp"] = new(
                "application/vnd.oasis.opendocument.presentation",
                stream => ValidateZipPackage(stream, "content.xml")),
        };

    public static string NormalizeAndValidateFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName.Trim());
        if (normalized.Length == 0
            || normalized.Length > MaximumFileNameLength
            || !string.Equals(fileName.Trim(), normalized, StringComparison.Ordinal)
            || normalized.Any(char.IsControl))
        {
            throw AttachmentProblem.Invalid("fileName", "The file name is invalid.");
        }

        if (!Rules.ContainsKey(Path.GetExtension(normalized)))
        {
            throw AttachmentProblem.Invalid("fileName", "The file extension is not permitted.");
        }

        return normalized;
    }

    public static string ValidateContentType(string fileName, string contentType)
    {
        var expected = Rules[Path.GetExtension(fileName)].ContentType;
        var normalized = contentType.Split(';', 2)[0].Trim();
        if (!string.Equals(normalized, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw AttachmentProblem.Invalid("contentType", $"The content type must be '{expected}'.");
        }

        return expected;
    }

    public static void ValidateContent(string fileName, Stream stream)
    {
        stream.Position = 0;
        try
        {
            Rules[Path.GetExtension(fileName)].Validator(stream);
        }
        catch (AttachmentValidationException exception)
        {
            throw AttachmentProblem.Invalid("file", exception.Message);
        }
        catch (Exception exception) when (exception is InvalidDataException
            or JsonException
            or XmlException
            or DecoderFallbackException)
        {
            throw AttachmentProblem.Invalid("file", "The file content does not match its declared format.");
        }
        finally
        {
            stream.Position = 0;
        }
    }

    private static void ValidatePdf(Stream stream) => RequirePrefix(stream, "%PDF-"u8);

    private static void ValidateJpeg(Stream stream) => RequirePrefix(stream, [0xff, 0xd8, 0xff]);

    private static void ValidatePng(Stream stream) =>
        RequirePrefix(stream, [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

    private static void ValidateWebp(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        if (stream.Read(header) != header.Length
            || !header[..4].SequenceEqual("RIFF"u8)
            || !header[8..].SequenceEqual("WEBP"u8))
        {
            throw new AttachmentValidationException("The file is not a valid WebP image.");
        }
    }

    private static void ValidateText(Stream stream)
    {
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var buffer = new char[4096];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (buffer.AsSpan(0, read).Contains('\0'))
            {
                throw new AttachmentValidationException("Text files must not contain null bytes.");
            }
        }
    }

    private static void ValidateJson(Stream stream)
    {
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64,
        });
    }

    private static void ValidateXml(Stream stream)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumFileSize,
        };
        using var reader = XmlReader.Create(stream, settings);
        while (reader.Read())
        {
        }
    }

    private static void ValidateZipPackage(Stream stream, string requiredEntry)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > 10_000
            || archive.GetEntry(requiredEntry) is null
            || archive.Entries.Any(entry =>
                entry.FullName.StartsWith("/", StringComparison.Ordinal)
                || entry.FullName.Contains("..", StringComparison.Ordinal)))
        {
            throw new AttachmentValidationException("The document package structure is invalid.");
        }
    }

    private static void RequirePrefix(Stream stream, ReadOnlySpan<byte> prefix)
    {
        Span<byte> actual = stackalloc byte[prefix.Length];
        if (stream.Read(actual) != prefix.Length || !actual.SequenceEqual(prefix))
        {
            throw new AttachmentValidationException("The file content does not match its extension.");
        }
    }

    private sealed record FileRule(string ContentType, Action<Stream> Validator);

    private sealed class AttachmentValidationException(string message) : Exception(message);
}
