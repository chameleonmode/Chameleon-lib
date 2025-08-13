namespace Tests;

public static class TestEnvironment
{
    public static readonly TestCredentials[] Directory = [
        new("jmutobu@outlook.com", "L5H7-UVPM-GVIC-VATA")
    ];
}

public record TestCredentials(string email, string license);
