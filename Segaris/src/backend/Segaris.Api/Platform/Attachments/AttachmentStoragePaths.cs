using Microsoft.Extensions.Options;
using Segaris.Api.Configuration;

namespace Segaris.Api.Platform.Attachments;

internal sealed class AttachmentStoragePaths
{
    public AttachmentStoragePaths(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        var configuredPath = options.Value.AttachmentsPath;
        Root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Path.GetTempPath(), "segaris", "attachments", environment.EnvironmentName)
            : configuredPath);
        Staging = Path.Combine(Root, ".staging");
        Trash = Path.Combine(Root, ".trash");
    }

    public string Root { get; }

    public string Staging { get; }

    public string Trash { get; }

    public string GetModuleDirectory(string module) => Path.Combine(Root, NormalizeModule(module));

    public string GetFilePath(string module, string storageFileName) =>
        Path.Combine(GetModuleDirectory(module), storageFileName);

    public static string NormalizeModule(string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        if (module.Length > 80
            || module.Any(character =>
                character is not (>= 'A' and <= 'Z')
                and not (>= 'a' and <= 'z')
                and not (>= '0' and <= '9')
                and not '-'))
        {
            throw AttachmentProblem.Invalid("owner", "The attachment owner module is invalid.");
        }

        return module.ToLowerInvariant();
    }
}
