using System.CommandLine;
using Vectra.Commands;

namespace Vectra.UnitTests.Commands;

public class VectraCommandLineTests
{
    [Fact]
    public void Create_ReturnsRootCommand()
    {
        var rootCommand = VectraCommandLine.Create([]);
        rootCommand.Should().NotBeNull().And.BeOfType<RootCommand>();
    }

    [Fact]
    public void Create_RootCommandHasVersionOption()
    {
        var rootCommand = VectraCommandLine.Create([]);
        // Options can be matched by any alias; check both long and short forms
        var hasVersion = rootCommand.Options.Any(o =>
            o.Name == "version" || o.Aliases.Contains("--version") || o.Aliases.Contains("-v"));
        hasVersion.Should().BeTrue();
    }

    [Fact]
    public void Create_BuiltInVersionOptionRemoved()
    {
        var rootCommand = VectraCommandLine.Create([]);
        // Confirm the custom --version option is present (exactly one option covering --version)
        var versionOptions = rootCommand.Options
            .Where(o => o.Name == "version" || o.Aliases.Contains("--version") || o.Aliases.Contains("-v"))
            .ToList();
        versionOptions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_VersionFlag_PrintsVersionAndReturns0()
    {
        var rootCommand = VectraCommandLine.Create([]);

        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);

        int exitCode;
        try
        {
            var parseResult = rootCommand.Parse(["--version"]);
            exitCode = await parseResult.InvokeAsync();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        exitCode.Should().Be(0);
        var output = writer.ToString();
        output.Should().Contain("Vectra");
    }

    [Fact]
    public void Create_WithArgs_DoesNotThrow()
    {
        var act = () => VectraCommandLine.Create(["--version"]);
        act.Should().NotThrow();
    }
}
