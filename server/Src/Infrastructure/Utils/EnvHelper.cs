using Domain.Exceptions;
using Domain.Interfaces.Utility;
using DotNetEnv;

namespace Infrastructure.Utils;

public class EnvHelper : IEnvHelper
{
    private static T? ParseOrThrow<T>(string value)
    {
        try
        {
            if (typeof(T) == typeof(bool))
            {
                var boolValue = bool.Parse(value);
                return (T)(object) boolValue;
            }
            if (typeof(T) == typeof(int))
            {
                var intValue = int.Parse(value);
                return (T)(object) intValue;
            }
            if (typeof(T) == typeof(char))
            {
                var charValue = char.Parse(value);
                return (T)(object) charValue;
            }
            if (typeof(T) == typeof(string))
            {
                return (T)(object) value;
            }
        }
        catch (FormatException formatException)
        {
            throw new FormatException($"The value '{value}' is not in valid format for '{typeof(T).Name}' type", formatException);
        }

        throw new WrongTypeEnvironmentVariableException($"Cannot convert type: {typeof(T).FullName} with value: {value}");
    }
    public T? Get<T>(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        return string.IsNullOrEmpty(value) ? throw new  EnvironmentVariableNotFoundException(key) : ParseOrThrow<T>(value);
    }

    public T GetOrDefault<T>(string key, T defaultValue)
    {
        try
        {
            var value = Get<T>(key);
            return value ?? defaultValue;
        }
        catch (EnvironmentVariableNotFoundException)
        {
            return defaultValue;
        }
    }

    public T GetRequired<T>(string key)
    {
        var value = Get<T>(key);
        return value ?? throw new RequireEnvironmentVariableException($"Variable {key} is required!");
    }
    public static string LoadAndGetConnectionString() => LoadAndGetConnectionString(false);

    public static string LoadAndGetConnectionString(bool throwIfNotFound)
    {
        const string connectionStringKey = "Database__PSqlConnectionString";
        
        // First check if already set in environment
        var existing = Environment.GetEnvironmentVariable(connectionStringKey);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }
        
        // Search for .env file upward from current directory
        var searchPaths = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."))
        };

        foreach (var basePath in searchPaths)
        {
            var envPath = Path.Combine(basePath, ".env");
            if (File.Exists(envPath))
            {
                try
                {
                    Env.Load(envPath);
                    var connectionString = Environment.GetEnvironmentVariable(connectionStringKey);
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        Console.WriteLine($"[EnvironmentHelper] Loaded {connectionStringKey} from {envPath}");
                        return connectionString;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EnvironmentHelper] Failed to load {envPath}: {ex.Message}");
                }
            }
        }
        
        // At the end, instead of silent fallback:
        if (throwIfNotFound)
        {
            throw new ConfigurationFailureException($"Connection string '{connectionStringKey}' not found");
        }
        
        return "Host=localhost;Port=5432;Database=Incident_Tracker;Username=postgres;Password=postgres";
    }
}