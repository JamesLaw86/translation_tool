using TranslationTool.Services;

namespace TranslationTool;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApiKeyEntry.Text = await SecureStorage.Default.GetAsync(TranslationService.ApiKeyStorageKey);
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		var key = ApiKeyEntry.Text?.Trim();
		if (string.IsNullOrEmpty(key))
			SecureStorage.Default.Remove(TranslationService.ApiKeyStorageKey);
		else
			await SecureStorage.Default.SetAsync(TranslationService.ApiKeyStorageKey, key);

		StatusLabel.Text = "Saved.";
		StatusLabel.IsVisible = true;
	}
}
