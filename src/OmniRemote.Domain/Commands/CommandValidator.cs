using System.Globalization;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Domain.Commands;

/// <summary>A command whose name, target and parameters have been checked against the device profile.</summary>
public sealed class ValidatedCommand
{
    private readonly IReadOnlyDictionary<string, string> _values;

    internal ValidatedCommand(DeviceCommand command, CommandSpec spec, IReadOnlyDictionary<string, string> values)
    {
        Command = command;
        Spec = spec;
        _values = values;
    }

    public DeviceCommand Command { get; }
    public CommandSpec Spec { get; }
    public string Name => Command.Name;
    public IReadOnlyDictionary<string, string> Values => _values;

    public bool GetBool(string name) => _values[name] == "true";
    public int GetInt(string name) => int.Parse(_values[name], NumberStyles.Integer, CultureInfo.InvariantCulture);
    public string GetString(string name) => _values[name];
}

public sealed record CommandValidationResult(bool IsValid, CommandErrorCode ErrorCode, string? Error, ValidatedCommand? Command)
{
    public static CommandValidationResult Invalid(CommandErrorCode code, string error) => new(false, code, error, null);
}

/// <summary>
/// Validates commands against device identity, registered capabilities and permitted ranges.
/// The same validator runs in the app and on the backend; receiving devices must validate independently too.
/// </summary>
public static class CommandValidator
{
    public const int MaxParameters = 8;
    public const int MaxParameterValueLength = 32;

    public static CommandValidationResult Validate(DeviceCommand command, RemoteDevice device)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(device);

        if (command.RequestId == Guid.Empty)
            return CommandValidationResult.Invalid(CommandErrorCode.ValidationFailed, "Request id is required.");
        if (command.DeviceId != device.Id)
            return CommandValidationResult.Invalid(CommandErrorCode.DeviceMismatch, "Command target does not match the device.");
        if (string.IsNullOrEmpty(command.Name) || !CommandCatalog.TryGet(command.Name, out var spec))
            return CommandValidationResult.Invalid(CommandErrorCode.UnknownCommand, "Unknown command.");
        if (!CommandCatalog.IsCapabilitySatisfied(spec, device.Profile))
            return CommandValidationResult.Invalid(CommandErrorCode.UnsupportedCapability, "This device does not support that operation.");
        if (command.Parameters.Count > MaxParameters)
            return CommandValidationResult.Invalid(CommandErrorCode.ValidationFailed, "Too many parameters.");

        foreach (var key in command.Parameters.Keys)
        {
            if (!spec.Parameters.Any(p => p.Name == key))
                return CommandValidationResult.Invalid(CommandErrorCode.ValidationFailed, "Unexpected parameter.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in spec.Parameters)
        {
            if (!command.Parameters.TryGetValue(p.Name, out var raw) || raw is null)
                return CommandValidationResult.Invalid(CommandErrorCode.ValidationFailed, $"Missing parameter '{p.Name}'.");
            if (raw.Length > MaxParameterValueLength)
                return CommandValidationResult.Invalid(CommandErrorCode.ValidationFailed, $"Parameter '{p.Name}' is too long.");

            var (ok, normalized) = ValidateParameter(p, raw, device.Profile);
            if (!ok)
                return CommandValidationResult.Invalid(CommandErrorCode.ValidationFailed, $"Parameter '{p.Name}' is out of range.");
            values[p.Name] = normalized;
        }

        return new(true, CommandErrorCode.None, null, new ValidatedCommand(command, spec, values));
    }

    private static (bool Ok, string Normalized) ValidateParameter(ParameterSpec p, string raw, DeviceProfile profile)
    {
        switch (p.Kind)
        {
            case ParameterKind.Toggle:
                return raw switch
                {
                    "true" => (true, "true"),
                    "false" => (true, "false"),
                    _ => (false, ""),
                };

            case ParameterKind.Number:
                if (!int.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var i)) return (false, "");
                var (min, max) = p.Binding == ProfileBinding.TemperatureRange
                    ? (profile.MinTemperatureC, profile.MaxTemperatureC)
                    : (p.Min, p.Max);
                if (min >= max && p.Binding == ProfileBinding.TemperatureRange) return (false, "");
                if (i < min || i > max || (p.DisallowZero && i == 0)) return (false, "");
                return (true, CommandCatalog.FormatInt(i));

            case ParameterKind.Choice:
                var allowed = p.Binding switch
                {
                    ProfileBinding.Modes => profile.Modes,
                    ProfileBinding.FanSpeeds => profile.FanSpeeds,
                    ProfileBinding.Inputs => profile.Inputs,
                    _ => p.AllowedValues ?? [],
                };
                return allowed.Contains(raw, StringComparer.Ordinal) ? (true, raw) : (false, "");

            case ParameterKind.ColorHex:
                if (raw.Length != 7 || raw[0] != '#') return (false, "");
                for (var k = 1; k < 7; k++)
                {
                    if (!char.IsAsciiHexDigit(raw[k])) return (false, "");
                }
                return (true, raw.ToUpperInvariant());

            default:
                return (false, "");
        }
    }
}
