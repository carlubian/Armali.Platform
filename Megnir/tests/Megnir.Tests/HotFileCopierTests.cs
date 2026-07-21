using Megnir.Backup;
using Megnir.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Megnir.Tests;

public class HotFileCopierTests
{
    private static HotFileCopier CreateCopier() => new(NullLogger<HotFileCopier>.Instance);

    /// <summary>Área de trabajo temporal autolimpiante para las fixtures del test.</summary>
    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; }

        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "megnir-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string At(params string[] parts) =>
            System.IO.Path.Combine(new[] { Root }.Concat(parts).ToArray());

        public string CreateDir(params string[] parts)
        {
            var p = At(parts);
            Directory.CreateDirectory(p);
            return p;
        }

        public string CreateFile(string content, params string[] parts)
        {
            var p = At(parts);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p)!);
            File.WriteAllText(p, content);
            return p;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // Best-effort: no romper la suite si el SO retiene un handle.
            }
        }
    }

    [Fact]
    public void CopyApp_copies_multiple_paths_recursively_under_basename()
    {
        using var ws = new TempWorkspace();

        // Origen: dos directorios con contenido recursivo.
        ws.CreateFile("hello", "src", "data", "f1.txt");
        ws.CreateFile("nested", "src", "data", "sub", "f2.txt");
        ws.CreateFile("cfg", "src", "config", "g.txt");

        var dest = ws.CreateDir("dest");
        var app = new AppEntry
        {
            Name = "app-a",
            Paths = { ws.At("src", "data"), ws.At("src", "config") },
        };

        var errors = CreateCopier().CopyApp(app, dest);

        Assert.Empty(errors);
        Assert.Equal("hello", File.ReadAllText(Path.Combine(dest, "data", "f1.txt")));
        Assert.Equal("nested", File.ReadAllText(Path.Combine(dest, "data", "sub", "f2.txt")));
        Assert.Equal("cfg", File.ReadAllText(Path.Combine(dest, "config", "g.txt")));
    }

    [Fact]
    public void CopyApp_records_partial_error_for_missing_path_and_copies_the_rest()
    {
        using var ws = new TempWorkspace();

        ws.CreateFile("hello", "src", "data", "f1.txt");
        var missing = ws.At("src", "does-not-exist");

        var dest = ws.CreateDir("dest");
        var app = new AppEntry
        {
            Name = "app-a",
            Paths = { missing, ws.At("src", "data") },
        };

        var errors = CreateCopier().CopyApp(app, dest);

        var error = Assert.Single(errors);
        Assert.Equal("app-a", error.App);
        Assert.Equal(missing, error.Path);
        Assert.Contains("no existe", error.Message);

        // La ruta válida se copió igualmente.
        Assert.Equal("hello", File.ReadAllText(Path.Combine(dest, "data", "f1.txt")));
    }

    [Fact]
    public void CopyApp_disambiguates_basename_collisions_with_suffix()
    {
        using var ws = new TempWorkspace();

        // Dos rutas distintas con el mismo basename "config".
        ws.CreateFile("first", "a", "config", "one.txt");
        ws.CreateFile("second", "b", "config", "two.txt");

        var dest = ws.CreateDir("dest");
        var app = new AppEntry
        {
            Name = "app-a",
            Paths = { ws.At("a", "config"), ws.At("b", "config") },
        };

        var errors = CreateCopier().CopyApp(app, dest);

        Assert.Empty(errors);
        Assert.Equal("first", File.ReadAllText(Path.Combine(dest, "config", "one.txt")));
        Assert.Equal("second", File.ReadAllText(Path.Combine(dest, "config-2", "two.txt")));
    }

    [Fact]
    public void CopyApp_copies_loose_file_directly_under_destination()
    {
        using var ws = new TempWorkspace();

        var file = ws.CreateFile("note-body", "src", "notes.txt");

        var dest = ws.CreateDir("dest");
        var app = new AppEntry { Name = "app-a", Paths = { file } };

        var errors = CreateCopier().CopyApp(app, dest);

        Assert.Empty(errors);
        // El fichero va directamente bajo <destino>/, no bajo un subdirectorio.
        Assert.Equal("note-body", File.ReadAllText(Path.Combine(dest, "notes.txt")));
        Assert.False(Directory.Exists(Path.Combine(dest, "notes.txt")));
    }

    [Fact]
    public void CopyApp_follows_directory_symlink_content()
    {
        using var ws = new TempWorkspace();

        var real = ws.CreateDir("real");
        ws.CreateFile("linked-content", "real", "inside.txt");
        var link = ws.At("link");

        try
        {
            Directory.CreateSymbolicLink(link, real);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Crear symlinks en Windows requiere privilegios (Developer Mode / admin). Si el
            // entorno no lo permite, el test se salta (retorno anticipado) en lugar de romper la
            // suite. xUnit 2.9 no expone Assert.Skip, así que se omite condicionalmente aquí.
            return;
        }

        var dest = ws.CreateDir("dest");
        var app = new AppEntry { Name = "app-a", Paths = { link } };

        var errors = CreateCopier().CopyApp(app, dest);

        Assert.Empty(errors);
        // El symlink se sigue: se copia el contenido real bajo el basename del enlace.
        Assert.Equal("linked-content", File.ReadAllText(Path.Combine(dest, "link", "inside.txt")));
    }
}
