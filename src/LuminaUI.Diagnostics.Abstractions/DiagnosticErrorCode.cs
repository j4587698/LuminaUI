namespace LuminaUI.Diagnostics.Abstractions;

public enum DiagnosticErrorCode
{
    InvalidRequest,
    UnknownMethod,
    TargetNotFound,
    UnsupportedOperation,
    ConversionFailed,
    UiThreadTimeout,
    TransportFailure,
    SerializationFailure,
    InternalError
}
