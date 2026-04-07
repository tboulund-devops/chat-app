using Domain.Exceptions;
using Infrastructure.Utils;

namespace Unit;

public class EnvHelperTests : IDisposable
{
    private readonly EnvHelper _envHelper = new();
    private readonly List<string> _setKeys = [];

    private void SetEnv(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value);
        _setKeys.Add(key);
    }

    public void Dispose()
    {
        foreach (var key in _setKeys)
            Environment.SetEnvironmentVariable(key, null);
    }

    // ── Get<T> ───────────────────────────────────────────────────────────

    [Fact]
    public void Get_ShouldReturnStringValue_WhenVariableExists()
    {
        SetEnv("TEST_STRING", "hello");
        var result = _envHelper.Get<string>("TEST_STRING");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Get_ShouldReturnIntValue_WhenVariableExists()
    {
        SetEnv("TEST_INT", "42");
        var result = _envHelper.Get<int>("TEST_INT");
        Assert.Equal(42, result);
    }

    [Fact]
    public void Get_ShouldReturnBoolValue_WhenVariableExists()
    {
        SetEnv("TEST_BOOL", "true");
        var result = _envHelper.Get<bool>("TEST_BOOL");
        Assert.True(result);
    }

    [Fact]
    public void Get_ShouldReturnCharValue_WhenVariableExists()
    {
        SetEnv("TEST_CHAR", "A");
        var result = _envHelper.Get<char>("TEST_CHAR");
        Assert.Equal('A', result);
    }

    [Fact]
    public void Get_ShouldThrowEnvironmentVariableNotFoundException_WhenKeyMissing()
    {
        Assert.Throws<EnvironmentVariableNotFoundException>(() =>
            _envHelper.Get<string>("NONEXISTENT_VAR_12345"));
    }

    [Fact]
    public void Get_ShouldThrowFormatException_WhenValueCannotBeParsed()
    {
        SetEnv("TEST_BAD_INT", "not_a_number");
        Assert.Throws<FormatException>(() => _envHelper.Get<int>("TEST_BAD_INT"));
    }

    [Fact]
    public void Get_ShouldThrowWrongTypeException_WhenTypeIsUnsupported()
    {
        SetEnv("TEST_UNSUPPORTED", "3.14");
        Assert.Throws<WrongTypeEnvironmentVariableException>(() =>
            _envHelper.Get<double>("TEST_UNSUPPORTED"));
    }

    // ── GetOrDefault<T> ──────────────────────────────────────────────────

    [Fact]
    public void GetOrDefault_ShouldReturnValue_WhenVariableExists()
    {
        SetEnv("TEST_DEFAULT_EXISTS", "actual");
        var result = _envHelper.GetOrDefault("TEST_DEFAULT_EXISTS", "fallback");
        Assert.Equal("actual", result);
    }

    // ── GetRequired<T> ──────────────────────────────────────────────────

    [Fact]
    public void GetRequired_ShouldReturnValue_WhenVariableExists()
    {
        SetEnv("TEST_REQUIRED", "value");
        var result = _envHelper.GetRequired<string>("TEST_REQUIRED");
        Assert.Equal("value", result);
    }

    [Fact]
    public void GetRequired_ShouldThrow_WhenVariableMissing()
    {
        Assert.Throws<EnvironmentVariableNotFoundException>(() =>
            _envHelper.GetRequired<string>("MISSING_REQUIRED_VAR_99999"));
    }
}
