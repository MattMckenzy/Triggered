using Microsoft.AspNetCore.Components;

namespace TownBulletin.Services
{
    public class GitHubService
    {
        private MarkupString? ReadmeMarkupString { get; set; }
        private IHttpClientFactory HttpClientFactory { get; }

        public GitHubService(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        public async Task<MarkupString> GetReadmeMarkupAsync()
        {
            if (ReadmeMarkupString == null)
            {
                HttpClient httpClient = HttpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "MattMckenzy");

                HttpResponseMessage responseMessage = await httpClient.GetAsync("https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/README.md");
                string markupString = await responseMessage.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(markupString))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    responseMessage = await httpClient.PostAsJsonAsync("https://api.github.com/markdown", new { text = markupString, mode = "gfm", context = "MattMckenzy/TownBulletin" });

                    if (responseMessage.IsSuccessStatusCode)
                        ReadmeMarkupString = new(await responseMessage.Content.ReadAsStringAsync());
                    else
                        return new MarkupString("Could not retrieve readme from GitHub, please try again later or create on issue at <a href=\"https://github.com/MattMckenzy/TownBulletin/issues\">MattMckenzy/TownBulletin</a>");
                }
            }

            return (MarkupString)ReadmeMarkupString!;
        }
    }
}
