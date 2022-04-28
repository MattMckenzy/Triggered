using Google.Cloud.TextToSpeech.V1;
using Google.Cloud.Translate.V3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Extensions;
using Triggered.Models;

namespace ModuleMaker.Utilities
{
    public static class GoogleUtilities
    {
        /// <summary>
        /// A list of defined languages supported for speech.
        /// </summary>
        public static IEnumerable<string> SupportedSpeechLanguages { get; } = new List<string>() { "fr", "en" };

        /// <summary>
        /// The default speech language, used if the text is in a language other than those supported.
        /// </summary>
        public static string DefaultSpeechLanguage { get; } = "fr-CA" ;

        /// <summary>
        /// Detects the text's language, translates it if it's in a langbuage other than those supported and synthesizes speech from it.
        /// </summary>
        /// <param name="text">The text from which to synthesize speech.</param>
        /// <param name="triggeredDbContext">An instancce of <see cref="TriggeredDbContext"/>, used to retrieve Google credentials, Google project ID and a temporary directory for the synthesized speech (keys: GoogleCredentialsPath, GoogleProjectId, GoogleSpeechDirectory).</param>
        /// <returns></returns>
        public static async Task<string?> DetectAndSynthesizeSpeech(string text, TriggeredDbContext triggeredDbContext)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", triggeredDbContext.Settings.GetSetting("GoogleCredentialsPath"));

            string? languageCode = await DetectLanguage(text, triggeredDbContext);
            if (languageCode != null)
            {
                if (SupportedSpeechLanguages.Any() && !SupportedSpeechLanguages.Any(supportedCode => languageCode.Contains(supportedCode, StringComparison.InvariantCultureIgnoreCase)))
                {
                    string? translatedText = await TranslateText(text, languageCode, DefaultSpeechLanguage, triggeredDbContext);

                    if (translatedText == null)
                        return null;
                    else
                        text = translatedText;

                    languageCode = DefaultSpeechLanguage;
                }                

                return await SynthesizeSpeech(text, triggeredDbContext.Settings.GetSetting("GoogleSpeechDirectory"), languageCode);
            }
            else
                return null;
        }

        private static async Task<string?> DetectLanguage(string text, TriggeredDbContext triggeredDbContext)
        {
            TranslationServiceClient client = TranslationServiceClient.Create();
            DetectLanguageRequest detectLanguageRequest = new()
            {
                Content = text,
                MimeType = "text/plain",
                Parent = $"projects/{triggeredDbContext.Settings.GetSetting("GoogleProjectId")}"
            };

            DetectLanguageResponse detectLanguageResponse = await client.DetectLanguageAsync(detectLanguageRequest);

            return detectLanguageResponse.Languages.FirstOrDefault()?.LanguageCode ?? null;
        }

        private static async Task<string?> TranslateText(string text, string sourceLanguageCode, string targetLanguageCode, TriggeredDbContext triggeredDbContext)
        {
            TranslationServiceClient client = TranslationServiceClient.Create();
            TranslateTextRequest translateTextRequest = new()
            {
                Contents = { text },
                MimeType = "text/plain",
                SourceLanguageCode = sourceLanguageCode,
                TargetLanguageCode = targetLanguageCode,
                Parent = $"projects/{triggeredDbContext.Settings.GetSetting("GoogleProjectId")}"
            };

            TranslateTextResponse detectLanguageResponse = await client.TranslateTextAsync(translateTextRequest);

            return detectLanguageResponse.Translations.FirstOrDefault()?.TranslatedText ?? null;
        }

        private static async Task<string?> SynthesizeSpeech(string text, string speechSavePath, string languageCode)
        {
            TextToSpeechClient client = TextToSpeechClient.Create();
            SynthesisInput synthesisInput = new()
            {
                Text = text
            };
            VoiceSelectionParams voiceSelection = new()
            {
                LanguageCode = languageCode,
                SsmlGender = SsmlVoiceGender.Unspecified,
            };
            AudioConfig audioConfig = new()
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            SynthesizeSpeechResponse response = await client.SynthesizeSpeechAsync(synthesisInput, voiceSelection, audioConfig);

            Directory.CreateDirectory(speechSavePath);
            string path = Path.Combine(speechSavePath, $"speech.mp3");

            using Stream output = File.Create(path);
            response.AudioContent.WriteTo(output);

            return path;
        }
    }
}