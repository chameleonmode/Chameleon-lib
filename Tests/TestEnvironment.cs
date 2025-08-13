using System;

namespace Tests;

public static class TestEnvironment
{
    // Load credentials from environment variables to avoid committing secrets.
    // Set CHAM_TEST_EMAIL and CHAM_TEST_LICENSE before running tests.
    public static readonly TestCredentials[] Directory = [
        new(
            Environment.GetEnvironmentVariable("CHAM_TEST_EMAIL") ?? string.Empty,
            Environment.GetEnvironmentVariable("CHAM_TEST_LICENSE") ?? string.Empty
        )
    ];
}

public record TestCredentials(string email, string license);
