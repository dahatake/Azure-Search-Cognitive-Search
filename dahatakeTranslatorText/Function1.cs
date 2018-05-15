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
		#region Execute Parameters
		static string host = "https://api.cognitive.microsofttranslator.com";
		static string path = "/translate?api-version=3.0";
		// Translate to Japanese.
		static string params_ = "&to=ja";

		static string uri = host + path + params_;


		// NOTE: Replace this example key with a valid subscription key.
		static string key = "<key>";
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


		#region Request
		public class cognitiveSkillRequest
		{
			public Value[] values { get; set; }
		}

		public class Value
		{
			public string recordId { get; set; }
			public Data data { get; set; }
		}

		public class Data
		{
			public string[] text { get; set; }
			public string language { get; set; }
		}
		#endregion


		#region Translator result object
		public class TranslationResult
		{
			public Detectedlanguage detectedLanguage { get; set; }
			public Translation[] translations { get; set; }
		}

		public class Detectedlanguage
		{
			public string language { get; set; }
			public float score { get; set; }
		}

		public class Translation
		{
			public string text { get; set; }
			public string to { get; set; }
		}

		#endregion

		/// <summary>
		/// Microsoft Translator Text call
		/// Limitation:
		///		1,000 charactor
		///		Single output -> it must to be multiple data input and output.
		/// </summary>
		[FunctionName("Translate")]
		public static IActionResult Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req,
			TraceWriter log)
		{

			try
			{

				log.Info("[Translate] Function started.");

				string requestBody = new StreamReader(req.Body).ReadToEnd();
				var requestBodyData = JsonConvert.DeserializeObject<cognitiveSkillRequest>(requestBody);

				log.Info($"[Translate] requestData:{requestBody}");

				// Put together response.
				WebApiEnricherResponse response = new WebApiEnricherResponse();
				response.values = new List<WebApiResponseRecord>();

				foreach (var data in requestBodyData.values)
				{
					string recordId = data.recordId;
					string originalText = data.data.text[0];

					var translatedText = TranslateText(originalText).Result;
					log.Info($"[Translate] response:{translatedText}");

					WebApiResponseRecord responseRecord = new WebApiResponseRecord();
					responseRecord.recordId = recordId;
					responseRecord.data = new Dictionary<string, object>();
					responseRecord.data.Add("text", translatedText);

					response.values.Add(responseRecord);

				}

				return (ActionResult)new OkObjectResult(response);
			}
			catch (Exception e)
			{
				log.Error($"Exception: {e.Message}");
				return new BadRequestObjectResult($"Exception: {e.Message}");
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
				var responseJSON = responseBody.Substring(1, responseBody.Length - 2);

				var result = JsonConvert.DeserializeObject<TranslationResult>(responseJSON);
				return result.translations[0].text;

				//dynamic data = JsonConvert.DeserializeObject(responseJSON);
				//var result = data?.Values?.First?.translations?.text?.Value as string;
				//return result;

			}

		}
	}
}
