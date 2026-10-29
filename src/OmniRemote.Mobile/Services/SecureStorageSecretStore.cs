using OmniRemote.Core.Abstractions;

namespace OmniRemote.Mobile.Services;

/// <summary>
/// Implements ISecretStore using MAUI platform secure storage
/// (Android Keystore + EncryptedSharedPreferences on Android; Keychain on iOS).
/// In non-platform testing / desktop fallback mode, uses an in-memory dictionary.
/// </summary>
public sealed class SecureStorageSecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _memoryFallback = new(StringComparer.Ordinal);

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (NotImplementedException)
        {
            return _memoryFallback.GetValueOrDefault(key);
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch (NotImplementedException)
        {
            _memoryFallback[key] = value;
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch (NotImplementedException)
        {
            _memoryFallback.Remove(key);
        }
        return Task.CompletedTask;
    }
}
