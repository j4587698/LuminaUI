using System.Threading.Tasks;
using Avalonia.Controls;
using LuminaUI.Controls;

namespace LuminaUI.Services;

public interface ILuminaDialogService
{
	void ShowDialog(object content, string? title = null);

	void ShowDialog(LuminaShell shell, object content, string? title = null);

	void ShowDialog(LuminaTopView topView, object content, string? title = null);

	void ShowDialog(Control owner, object content, string? title = null);

	void ShowTopDialog(Control owner, object content, string? title = null);

	Task<bool> ShowConfirmAsync(string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false);

	Task<bool> ShowConfirmAsync(LuminaShell shell, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false);

	Task<bool> ShowConfirmAsync(LuminaTopView topView, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false);

	Task<bool> ShowConfirmAsync(Control owner, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false);

	Task<bool> ShowTopConfirmAsync(Control owner, string title, string message, string? confirmText = null, string? cancelText = null, bool isDanger = false);

	void CloseDialog();

	void CloseDialog(LuminaShell shell);

	void CloseDialog(LuminaTopView topView);

	void CloseDialog(Control owner);

	void CloseTopDialog(Control owner);
}
