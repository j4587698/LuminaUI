using System.Globalization;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using LuminaUI.Diagnostics.Controls;
using LuminaUI.Diagnostics.Abstractions;

namespace LuminaUI.Diagnostics.Inspection;

internal static class InspectionRequestHelpers
{
    public static IReadOnlyList<Control> GetCurrentWindowRoots() =>
        WindowInspectionHandler.GetCurrentWindows().Cast<Control>().ToArray();

    public static int GetInt(
        JsonObject parameters,
        string name,
        int defaultValue)
    {
        if (!parameters.TryGetPropertyValue(name, out var value) || value is null)
            return defaultValue;

        try
        {
            return value.GetValue<int>();
        }
        catch (Exception) when (
            value is JsonValue)
        {
            return defaultValue;
        }
    }

    public static double? GetDouble(
        JsonObject parameters,
        string name)
    {
        if (!parameters.TryGetPropertyValue(name, out var value) || value is null)
            return null;

        try
        {
            return value.GetValue<double>();
        }
        catch (Exception) when (
            value is JsonValue)
        {
            var text = GetString(parameters, name);
            return double.TryParse(text, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }

    public static string? GetString(
        JsonObject parameters,
        string name)
    {
        if (!parameters.TryGetPropertyValue(name, out var value) || value is null)
            return null;

        try
        {
            return value.GetValue<string>();
        }
        catch (Exception) when (
            value is JsonValue)
        {
            return null;
        }
    }

    public static bool GetBool(
        JsonObject parameters,
        string name,
        bool defaultValue = false)
    {
        if (!parameters.TryGetPropertyValue(name, out var value) || value is null)
            return defaultValue;

        try
        {
            return value.GetValue<bool>();
        }
        catch (Exception) when (
            value is JsonValue)
        {
            var text = GetString(parameters, name);
            return bool.TryParse(text, out var parsed)
                ? parsed
                : defaultValue;
        }
    }

    public static string[] GetStringArray(
        JsonObject parameters,
        string name)
    {
        if (!parameters.TryGetPropertyValue(name, out var value) || value is not JsonArray array)
            return [];

        return array
            .Select(item =>
            {
                try
                {
                    return item?.GetValue<string>();
                }
                catch (Exception)
                {
                    return null;
                }
            })
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    public static ControlLookup ResolveControl(
        DiagnosticRequest request,
        IReadOnlyList<Control> roots,
        IControlResolver resolver)
    {
        var controlId = GetString(request.Parameters, "controlId");
        if (string.IsNullOrWhiteSpace(controlId))
        {
            return ControlLookup.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    "Parameter 'controlId' is required."));
        }

        if (!ControlIdentifierParser.TryParse(controlId, out var identifier, out var error))
        {
            return ControlLookup.Failed(
                DiagnosticResponse.Fail(
                    request.Id,
                    DiagnosticErrorCode.InvalidRequest,
                    error!,
                    new JsonObject { ["controlId"] = controlId }));
        }

        for (var rootIndex = 0; rootIndex < roots.Count; rootIndex++)
        {
            var resolution = resolver.Resolve(roots[rootIndex], identifier);
            if (resolution.Found)
                return ControlLookup.Found(resolution.Control!, rootIndex, controlId);
        }

        return ControlLookup.Failed(
            DiagnosticResponse.Fail(
                request.Id,
                DiagnosticErrorCode.TargetNotFound,
                $"Control '{controlId}' was not found.",
                new JsonObject { ["controlId"] = controlId }));
    }
}

internal sealed record ControlLookup(
    bool Success,
    Control? Control,
    int RootIndex,
    string? ControlId,
    DiagnosticResponse? Response)
{
    public static ControlLookup Found(
        Control control,
        int rootIndex,
        string controlId) =>
        new(Success: true, control, rootIndex, controlId, Response: null);

    public static ControlLookup Failed(DiagnosticResponse response) =>
        new(Success: false, Control: null, RootIndex: -1, ControlId: null, response);
}
