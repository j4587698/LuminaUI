namespace LuminaUI.Controls;

public enum LuminaDialogResult
{
    /// <summary>
    /// Nothing is returned from the dialog box. This means that the modal dialog continues running.
    /// </summary>
    None,

    /// <summary>
    /// The dialog box return value is OK (usually sent from a button labeled OK).
    /// </summary>
    Ok,

    /// <summary>
    /// The dialog box return value is Cancel (usually sent from a button labeled Cancel).
    /// </summary>
    Cancel,

    /// <summary>
    /// The dialog box return value is Yes (usually sent from a button labeled Yes).
    /// </summary>
    Yes,

    /// <summary>
    /// The dialog box return value is No (usually sent from a button labeled No).
    /// </summary>
    No
}
