using Segaris.Api.Platform.Observability;

namespace Segaris.UnitTests;

public sealed class FrontendDiagnosticRedactorTests
{
    [Theory]
    [InlineData("password=Secret123!", "password=[REDACTED]")]
    [InlineData("apiKey:abc123", "apiKey:[REDACTED]")]
    [InlineData("Authorization: Bearer token-value", "Authorization: [REDACTED]")]
    [InlineData("cookie=session-value", "cookie=[REDACTED]")]
    [InlineData(
        "connectionString=Host=db;Password=secret",
        "connectionString=[REDACTED];Password=[REDACTED]")]
    public void Known_secret_shapes_are_redacted(string input, string expected)
    {
        Assert.Equal(expected, FrontendDiagnosticRedactor.Redact(input));
    }

    [Fact]
    public void Ordinary_diagnostic_text_is_preserved()
    {
        const string message = "Application initialization failed at route /dashboard.";

        Assert.Equal(message, FrontendDiagnosticRedactor.Redact(message));
    }
}
