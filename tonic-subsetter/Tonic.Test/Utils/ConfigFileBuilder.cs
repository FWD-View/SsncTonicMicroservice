using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tonic.Common.Utils;

namespace Tonic.Test.Utils;

public record ConfigFileBuilder
{
    private List<string> Files { get; }= new();
    private List<string> Contents { get; } = new();

    public static ConfigFileBuilder WithSubsetterOptions()
    {
        var builder = new ConfigFileBuilder{};
        builder.Files.Add("subsetter_options_standard.toml");
        return builder;
    }

    public static ConfigFileBuilder WithPrimaryKeyRemapperOptions()
    {
        var builder = new ConfigFileBuilder{};
        builder.Files.Add("pkremapper_options_standard.toml");
        return builder;
    }

    public ConfigFileBuilder WithTonicHost()
    {
        Files.Add("lh_test_db.toml");
        return this;
    }

    public ConfigFileBuilder WithFiles(params string[] files)
    {
        Files.AddRange(files);
        return this;
    }
    
    public ConcatenatedStream ToConcatenatedStream() =>
        new(Files.Select(f =>
                Path.Combine(Directory.GetCurrentDirectory(), "sample_configs", f))
            .Select(f => new FileStream(f, FileMode.Open)));
}