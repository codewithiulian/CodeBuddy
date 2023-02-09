using CodeBuddy.Data;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Octokit;
using System.Net.Http.Headers;
using System.Text;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace CodeBuddy.Pages
{
    public partial class Index
	{
		[Inject] private IHttpClientFactory ClientFactory { get; set; }
		[Inject] private IConfiguration Configuration { get; set; }

		private SearchModel searchModel = new();
		private bool isExecuting = false;
		private bool isBuilder = false;
		private string newWebsiteUrl = "";

		protected async Task Execute()
		{
			if (string.IsNullOrEmpty(searchModel.Prompt))
			{
				searchModel.Prompt = "Please ask a question, i.e: write a simple record in c# called Person";
			}

			isExecuting = true;

			HttpRequestMessage request = new(HttpMethod.Post,
				"https://api.openai.com/v1/completions");
			var client = ClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", Configuration["OpenAPIToken"]);

			RequestModel requestModel = new(
				model: "text-davinci-003",
				prompt: searchModel.Prompt,
				max_tokens: 2048, // ~ 1500 words
				temperature: 0, // 0: exact and repetitive. 1: creative and random
				frequency_penalty: 0.2 // -2 to 2. The smaller the number, the less likely to repeat lines
			);

			var requestModelJson = JsonConvert.SerializeObject(requestModel);
			request.Content = new StringContent(requestModelJson, Encoding.UTF8, "application/json");

			var result = await client.SendAsync(request);

			isExecuting = false;

			if (result.IsSuccessStatusCode)
			{
				var response = await result.Content.ReadFromJsonAsync<ResponseModel>();

				if (response is not null && response.choices.Any()
					&& !string.IsNullOrEmpty(response.choices[0].text))
				{
					var responseText = response.choices[0].text;

					if (isBuilder)
					{
						HtmlDocument htmlDoc = new();
						htmlDoc.LoadHtml(responseText);

						if (htmlDoc.ParseErrors.Any())
						{
							searchModel.Prompt += "No valid html response. See below:";
							searchModel.Prompt += responseText;
						}
						else
						{
							GitHubClient gitHubClient = new(new ProductHeaderValue("CodeBuddy"));
							gitHubClient.Credentials = new Credentials(Configuration["CodeBuddyToken"]);
							var pathExtension = Guid.NewGuid();
							var owner = "iulianoana";
							var repoName = "CodeBuddy-1";
							var filePath = $"websites/{pathExtension}.html";
							var branch = "main";
							var generatedUrl = $"https://yellow-glacier-016494c10.2.azurestaticapps.net/websites/{pathExtension}.html";
							var createFileRequest = new CreateFileRequest($"Inserting new website {pathExtension}", responseText, branch);

							await gitHubClient.Repository.Content.CreateFile(owner, repoName, filePath, createFileRequest);

							newWebsiteUrl = generatedUrl;
						}
					}
					else
					{
						searchModel.Prompt += responseText;
					}
				}
				else
				{
					searchModel.Prompt += "No results for this search. Please try again";
				}
			}
		}
	}
}
