using TranslationTool.Services;

namespace TranslationTool;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnTranslateClicked(object? sender, EventArgs e)
	{
		var text = InputEditor.Text?.Trim();
		if (string.IsNullOrEmpty(text))
			return;

		InputEditor.Unfocus();
		SetBusy(true);
		ResultsView.ItemsSource = null;

		try
		{
			ResultsView.ItemsSource = await TranslationService.GetAlternativesAsync(text);
			ShowStatus("Tap a translation to copy it.");
		}
		catch (TranslationException ex)
		{
			ShowStatus(ex.Message);
		}
		catch (Exception ex)
		{
			ShowStatus($"Something went wrong: {ex.Message}");
		}
		finally
		{
			SetBusy(false);
		}
	}

	private async void OnResultTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is string french)
		{
			await Clipboard.Default.SetTextAsync(french);
			ShowStatus("Copied!");
		}
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new SettingsPage());
	}

	private void SetBusy(bool busy)
	{
		Busy.IsRunning = busy;
		Busy.IsVisible = busy;
		TranslateButton.IsEnabled = !busy;
		if (busy)
			StatusLabel.IsVisible = false;
	}

	private void ShowStatus(string message)
	{
		StatusLabel.Text = message;
		StatusLabel.IsVisible = true;
	}
}
