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

namespace CognitiveSearchCustomSkill
{

	public static class FaceIdentify
	{

		#region Parameters
		static string key = Environment.GetEnvironmentVariable("FACEAPI_KEY");
		static string region = Environment.GetEnvironmentVariable("FACEAPI_REGION"); //southeastasia
		static string personGroupId = Environment.GetEnvironmentVariable("FACEAPI_GROUPID"); //msft
		static string detectUri = $"https://{region}.api.cognitive.microsoft.com/face/v1.0/detect";
		static string identifyUri = $"https://{region}.api.cognitive.microsoft.com/face/v1.0/identify";
		static string personUri = $"https://{region}.api.cognitive.microsoft.com/face/v1.0/persongroups/{personGroupId}/persons/";


		static string Test_imageFile = @"C:\Work\DaiyuHatakeyama201708_500k.jpg";
		#endregion

		#region use to create Face Identify request

		public class faceIdeneityRequest
		{
			public string personGroupId { get; set; }
			public string[] faceIds { get; set; }
			public int maxNumOfCandidatesReturned { get; set; }
			public float confidenceThreshold { get; set; }
		}

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

		[FunctionName("FaceIdentify")]
		public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
		{

			try
			{

				log.Info("[FaceIdentify] started");

				string name="";

				// 1. Parameter Validations
				if (key == null)
					return new BadRequestObjectResult($"[FaceIdentify][Error] TranslatorText KEY is missing in Environment Variable.");

				string requestBody = new StreamReader(req.Body).ReadToEnd();
				log.Info($"[FaceIdentify] Request Data:{requestBody}");

				dynamic data = JsonConvert.DeserializeObject(requestBody);
				var recordId = data?.values?.First?.recordId?.Value as string;
				var imagedata = data?.values?.First?.data?.image?.data.Value as string;
				log.Info($"[FaceIdentify] recordId:{recordId}, imagedata:{imagedata}");

				byte[] bytedata = System.Convert.FromBase64String(imagedata);
				Stream s = new MemoryStream(bytedata);

				var faceID = detectFace(s).Result;
				log.Info($"[FaceIdentify] faceID:{faceID}");
				if (faceID != null)
				{
					var personID = identifyFace(faceID).Result;
					log.Info($"[FaceIdentify] personID:{personID}");
					if (personID != null)
					{
						name = getPerson(personID).Result;
						log.Info($"[FaceIdentify] name:{name}");
					}

				}

				// 3. Build response JSON
				WebApiResponseRecord responseRecord = new WebApiResponseRecord();
				responseRecord.recordId = recordId;
				responseRecord.data = new Dictionary<string, object>();
				responseRecord.data.Add("name", name);

				// Put together response.
				WebApiEnricherResponse response = new WebApiEnricherResponse();
				response.values = new List<WebApiResponseRecord>();
				response.values.Add(responseRecord);

				log.Info($"[FaceIdentify] Complate.");

				return (ActionResult)new OkObjectResult(response);
			}
			catch (Exception e)
			{
				log.Error($"[FaceIdentify] Exception: {e.Message}");
				return new BadRequestObjectResult($"[FaceIdentify] Exception: {e.Message}");
			}

		}

		async static Task<string> detectFace(Stream imageData)
		{
			using (var client = new HttpClient())
			using (var request = new HttpRequestMessage())
			{
				byte[] FileByteArrayData = new byte[imageData.Length];
				imageData.Read(FileByteArrayData, 0, System.Convert.ToInt32(imageData.Length));
				imageData.Close();

				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri(detectUri);
				request.Content = new ByteArrayContent(FileByteArrayData);
				request.Headers.Add("Ocp-Apim-Subscription-Key", key);
				request.Content.Headers.Add("Content-Type", "application/octet-stream");

				var response = await client.SendAsync(request);
				var responseBody = await response.Content.ReadAsStringAsync();

				dynamic data = JsonConvert.DeserializeObject(responseBody);
				var faceID = data?.First?.faceId.Value as string;

				return faceID;
			}

		}

		async static Task<string> identifyFace(string faceID)
		{
			using (var client = new HttpClient())
			using (var request = new HttpRequestMessage())
			{
				faceIdeneityRequest faceIdentifyreq = new faceIdeneityRequest
				{
					personGroupId = personGroupId,
					faceIds = new string[1] {
							faceID
						},
					maxNumOfCandidatesReturned = 1,
					confidenceThreshold = 0.5F
				};

				var requestBody = JsonConvert.SerializeObject(faceIdentifyreq);

				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri(identifyUri);
				request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
				request.Headers.Add("Ocp-Apim-Subscription-Key", key);

				var response = await client.SendAsync(request);
				var responseBody = await response.Content.ReadAsStringAsync();

				dynamic data = JsonConvert.DeserializeObject(responseBody);
				var personID = data?.First?.candidates?.First?.personId?.Value as string;

				return personID;
			}

		}

		async static Task<string> getPerson(string personID)
		{
			using (var client = new HttpClient())
			using (var request = new HttpRequestMessage())
			{

				request.Method = HttpMethod.Get;
				request.RequestUri = new Uri(personUri + personID);
				request.Headers.Add("Ocp-Apim-Subscription-Key", key);

				var response = await client.SendAsync(request);
				var responseBody = await response.Content.ReadAsStringAsync();

				dynamic data = JsonConvert.DeserializeObject(responseBody);
				var name = data?.name?.Value as string;

				return name;
			}

		}

	}
}
