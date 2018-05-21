using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System;


namespace dahatakeTranslatorText
{
	// This function will simply translate messages sent to it.
	public static class Function1
	{
		#region Parameters
		static readonly string uri = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=ja";
		static readonly string key = Environment.GetEnvironmentVariable("TRANSLATOR_TEXT_KEY");
		#endregion

		#region classes used to serialize the response
		private class WebApiResponseError
		{
			public string message { get; set; }
		}

		private class WebApiResponseWarning
		{
			public string message { get; set; }
		}

		private class WebApiResponseRecord
		{
			public string recordId { get; set; }
			public Dictionary<string, object> data { get; set; }
			public List<WebApiResponseError> errors { get; set; }
			public List<WebApiResponseWarning> warnings { get; set; }
		}

		private class WebApiEnricherResponse
		{
			public List<WebApiResponseRecord> values { get; set; }
		}
		#endregion

		/// <summary>
		/// Microsoft Translator Text call
		/// Limitation:
		///		1,000 charactor
		///		Single output -> it must to be multiple data input and output.
		/// </summary>
		/// <
		[FunctionName("Translate")]
		public static IActionResult Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req,
			TraceWriter log)
		{

			try
			{
				string recordId = null;
				string originalText = null;
				string translatedText = null;

				log.Info("[Translate] Function started.");

				// 1. Parameter Validations
				if (key == null)
					return new BadRequestObjectResult($"[Translate][Error] TranslatorText KEY is missing in Environment Variable.");

				string requestBody = new StreamReader(req.Body).ReadToEnd();
				log.Info($"[Translate] Request Data:{requestBody}");

				dynamic data = JsonConvert.DeserializeObject(requestBody);
				if (data?.values == null)
				{
					return new BadRequestObjectResult("[Translate] Could not find values array");
				}
				if (data?.values.HasValues == false || data?.values.First.HasValues == false)
				{
					// It could not find a record, then return empty values array.
					return new BadRequestObjectResult("[Translate] Could not find valid records in values array");
				}

				recordId = data?.values?.First?.recordId?.Value as string;
				if (recordId == null)
				{
					return new BadRequestObjectResult("[Translate] recordId cannot be null");
				}

				originalText = data?.values?.First?.data?.text?.Value as string;
				if (originalText == null)
				{
					return new BadRequestObjectResult("[Translate] text cannot be null");
				}

				// 1.1. Text clearnup for TextSplit task
				originalText = originalText.Replace("\n", "").Trim();
				log.Info($"[Translate] OriginalText:{originalText}");

				// 2. Call Translator Text
				translatedText = TranslateText(originalText).Result;
				log.Info($"[Translate] TranslatedData: id:{recordId} - text:{translatedText}");

				// 3. Build response JSON
				WebApiResponseRecord responseRecord = new WebApiResponseRecord();
				responseRecord.recordId = recordId;
				responseRecord.data = new Dictionary<string, object>();
				responseRecord.data.Add("text", translatedText);

				// Put together response.
				WebApiEnricherResponse response = new WebApiEnricherResponse();
				response.values = new List<WebApiResponseRecord>();
				response.values.Add(responseRecord);

				log.Info($"[Translate] Complate.");

				return (ActionResult)new OkObjectResult(response);
			}
			catch (Exception e)
			{
				log.Error($"[Translate] Exception: {e.Message}");
				return new BadRequestObjectResult($"[Translate] Exception: {e.Message}");
			}
		}

		/// <summary>
		/// Use Cognitive Service to translate text from one language to antoher.
		/// </summary>
		/// <param name="myText">The text to translate</param>
		/// <param name="destinationLanguage">The language you want to translate to.</param>
		/// <returns>Asynchronous task that returns the translated text. </returns>
		async static Task<string> TranslateText(string text)
		{

			System.Object[] body = new System.Object[] { new { Text = text } };
			var requestBody = JsonConvert.SerializeObject(body);

			using (var client = new HttpClient())
			using (var request = new HttpRequestMessage())
			{
				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri(uri);
				request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
				request.Headers.Add("Ocp-Apim-Subscription-Key", key);

				var response = await client.SendAsync(request);
				var responseBody = await response.Content.ReadAsStringAsync();

				dynamic data = JsonConvert.DeserializeObject(responseBody);
				var result = data?.First?.translations?.First?.text?.Value as string;
				result = result.Replace("ÅE", "").Trim();

				return result;

			}

		}
	}
}
