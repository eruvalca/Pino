using System.Diagnostics.CodeAnalysis;

namespace Pino.BrowserTests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit evaluates public conditional-skip properties.")]
public static class BrowserEnvironment
{
    public static bool Enabled => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PINO_BROWSER_URL")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PINO_MAILPIT_URL")) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE"));
}
