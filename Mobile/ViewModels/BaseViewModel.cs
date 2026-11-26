using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Ecommerce.Mobile.Services;

namespace Ecommerce.Mobile.ViewModels;

public abstract partial class BaseViewModel(IUiService ui) : ObservableObject
{
    protected IUiService Ui { get; } = ui;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Called by the page every time it appears.</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs a data-loading operation: toggles <see cref="IsBusy"/> and stores failures in
    /// <see cref="ErrorMessage"/> so the page can render an inline error state.
    /// </summary>
    protected async Task<bool> RunAsync(Func<Task> action, bool showBusy = true)
    {
        try
        {
            if (showBusy) IsBusy = true;
            ErrorMessage = null;
            await action();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ToMessage(ex);
            return false;
        }
        finally
        {
            if (showBusy) IsBusy = false;
        }
    }

    /// <summary>Runs a user action (button press) and reports failures in an alert dialog.</summary>
    protected async Task<bool> RunActionAsync(Func<Task> action, string errorTitle = "Something went wrong")
    {
        try
        {
            IsBusy = true;
            await action();
            return true;
        }
        catch (ApiException ex) when (ex.IsUnauthorized)
        {
            return false; // the global session-expired handler shows the message and redirects to login
        }
        catch (Exception ex)
        {
            await Ui.AlertAsync(errorTitle, ToMessage(ex));
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected static string ToMessage(Exception ex)
    {
        if (ex is ApiException api)
            return api.Message;

        Debug.WriteLine(ex);
        return "An unexpected error occurred. Please try again.";
    }
}
