// TEMPLATE: Endpoint seed helper via HTTP POST
// Replace placeholders in brackets with your module and entity names.

namespace [RootNamespace].Modules.[Module].IntegrationTests.Presentation.Web;

using System.Net.Mime;
using System.Text;
using System.Text.Json;

public static class [Entity]SeedHelper
{
    public static async Task<[Entity]Model> SeedEntityAsync(HttpClient client, string route, [Entity]Model model)
    {
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await client.PostAsync(route, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<[Entity]Model>();
    }
}
