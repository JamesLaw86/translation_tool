using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;

namespace TranslationTool.Services;

public record FrenchAlternative(
	[property: JsonPropertyName("french")] string French,
	[property: JsonPropertyName("register")] string Register,
	[property: JsonPropertyName("note")] string Note);

record AlternativesResponse(
	[property: JsonPropertyName("alternatives")] List<FrenchAlternative> Alternatives);

public class TranslationException(string message) : Exception(message);

public static class TranslationService
{
	public const string ApiKeyStorageKey = "anthropic_api_key";

	const string SystemPrompt = """
		You help an English speaker who is learning French see the range of natural ways to say something in French.

		The user sends a sentence, usually in English. If it is already in French, treat it as the meaning to rephrase.

		Return between 4 and 8 genuinely different French versions that a native speaker might actually say. Spread them
		across registers (formal, neutral, informal, slang) where that is natural for the sentence. Vary the vocabulary and
		sentence structure, not just tu/vous. Put the most common, natural version first.

		For each version, write a short note in English (one sentence) explaining its nuance, when you would use it, or
		anything a learner would find surprising, such as a literal back-translation of an idiom.
		""";

	static readonly Dictionary<string, JsonElement> Schema = new()
	{
		["type"] = JsonSerializer.SerializeToElement("object"),
		["properties"] = JsonSerializer.SerializeToElement(new
		{
			alternatives = new
			{
				type = "array",
				items = new
				{
					type = "object",
					properties = new
					{
						french = new { type = "string" },
						register = new { type = "string", @enum = new[] { "formal", "neutral", "informal", "slang" } },
						note = new { type = "string" },
					},
					required = new[] { "french", "register", "note" },
					additionalProperties = false,
				},
			},
		}),
		["required"] = JsonSerializer.SerializeToElement(new[] { "alternatives" }),
		["additionalProperties"] = JsonSerializer.SerializeToElement(false),
	};

	public static async Task<List<FrenchAlternative>> GetAlternativesAsync(string text, CancellationToken cancellationToken = default)
	{
		var apiKey = await SecureStorage.Default.GetAsync(ApiKeyStorageKey);
		if (string.IsNullOrWhiteSpace(apiKey))
			throw new TranslationException("Add your Anthropic API key in Settings first.");

		AnthropicClient client = new() { ApiKey = apiKey };

		BetaMessage response;
		try
		{
			response = await client.Beta.Messages.Create(new MessageCreateParams
			{
				Model = "claude-haiku-4-5",
				MaxTokens = 4096,
				System = SystemPrompt,
				OutputConfig = new BetaOutputConfig
				{
					Format = new BetaJsonOutputFormat { Schema = Schema },
				},
				Messages = [new() { Role = Role.User, Content = text }],
			}, cancellationToken);
		}
		catch (AnthropicUnauthorizedException)
		{
			throw new TranslationException("Your API key was rejected. Check it in Settings.");
		}
		catch (AnthropicRateLimitException)
		{
			throw new TranslationException("Too many requests. Wait a moment and try again.");
		}
		catch (AnthropicApiException ex)
		{
			throw new TranslationException($"The request failed: {ex.Message}");
		}
		catch (AnthropicIOException)
		{
			throw new TranslationException("Couldn't reach the server. Check your internet connection.");
		}

		if (response.StopReason == "refusal")
			throw new TranslationException("Claude declined to translate that text.");

		var json = string.Concat(response.Content
			.Select(b => b.TryPickText(out var t) ? t.Text : null)
			.Where(t => t is not null));

		var parsed = JsonSerializer.Deserialize<AlternativesResponse>(json);
		if (parsed is null || parsed.Alternatives.Count == 0)
			throw new TranslationException("Got an empty response. Try again.");

		return parsed.Alternatives;
	}
}
