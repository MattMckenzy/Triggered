using Microsoft.AspNetCore.Components;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that offers ways to interact with GitHub API.
    /// </summary>
    public class GitHubService
    {
        private MarkupString? ReadmeMarkupString { get; set; }
        private IHttpClientFactory HttpClientFactory { get; }

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>
        /// <param name="httpClientFactory">An injected <see cref="IHttpClientFactory"/>.</param>
        public GitHubService(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Retrieves the README markup from "MattMckenzy/Triggered" as an HTML <see cref="MarkupString"/>.
        /// </summary>
        /// <returns>The README HTML <see cref="MarkupString"/>.</returns>
        public async Task<MarkupString> GetReadmeMarkupAsync()
        {
            if (ReadmeMarkupString == null)
            {
                HttpClient httpClient = HttpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "MattMckenzy");

                HttpResponseMessage responseMessage = await httpClient.GetAsync("https://raw.githubusercontent.com/MattMckenzy/Triggered/main/README.md");
                string markupString = await responseMessage.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(markupString))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    responseMessage = await httpClient.PostAsJsonAsync("https://api.github.com/markdown", new { text = markupString, mode = "gfm", context = "MattMckenzy/Triggered" });

                    if (responseMessage.IsSuccessStatusCode)
                        ReadmeMarkupString = new(await responseMessage.Content.ReadAsStringAsync());
                    else
                        return new MarkupString("Could not retrieve readme from GitHub, please try again later or create on issue at <a href=\"https://github.com/MattMckenzy/Triggered/issues\">MattMckenzy/Triggered</a>");
                }
            }

            return (MarkupString)ReadmeMarkupString!;
        }
    }
}
