using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RegisterFaceAPIPersonWithGroup
{
	class Program
	{
		static void Main(string[] args)
		{

			// check arg input
			if (args.Length < 5)
			{
				Console.WriteLine("argument missing");
				Console.WriteLine("  Usage: RegisterFaceAPIPersonWithGroup [Face API key] [Face API Region] [Person Group Id] [Person Name] [Person Image Folder Path]");
			}

			Console.WriteLine($"Start Registration - person: {args[3]}");
			try
			{
				var result = registerPerson(args[0], args[1], args[2], args[3], args[4]);
				result.Wait();
				Console.WriteLine("Person with Images are successfully registered!!!");

			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
			}

			Console.WriteLine("Push any key to terminate program.");
			Console.Read();

		}

		async static Task registerPerson(string faceAPIKey, 
												string faceAPIRegion,
												string personGroupId,
												string personName,
												string personImageFolderPath)
		{

			Console.WriteLine("  Connect Face API");
			var faceServiceClient =
				new FaceServiceClient(faceAPIKey, 
				$"https://{faceAPIRegion}.api.cognitive.microsoft.com/face/v1.0");

			Console.WriteLine("  Create | Get Person Group");
			try
			{
				await faceServiceClient.DeletePersonGroupAsync(personGroupId);
				var presonGroup = await faceServiceClient.GetPersonGroupAsync(personGroupId);
			} catch	(FaceAPIException e)
			{
				if (e.ErrorCode == "PersonGroupNotFound")
				{
					Console.WriteLine("  Create Person Group");
					await faceServiceClient.CreatePersonGroupAsync(personGroupId, personGroupId);
				}
				else {
					Console.WriteLine($"  Exception - GetPersonGroupAsync: {e.Message}");
					return;
				}
			}

			Console.WriteLine("  Add Person");
				CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(
					personGroupId,
					personName
				);

			Console.WriteLine("  Add Images to Person");
			foreach (string imagePath in Directory.GetFiles(personImageFolderPath, "*.jpg")
				.Concat(Directory.GetFiles(personImageFolderPath, "*.png"))
				.Concat(Directory.GetFiles(personImageFolderPath, "*.gif"))
				.Concat(Directory.GetFiles(personImageFolderPath, "*.bmp"))
				.ToArray())
			{
				using (Stream s = File.OpenRead(imagePath))
				{
					// Detect faces in the image and add to Anna
					await faceServiceClient.AddPersonFaceAsync(
						personGroupId, friend1.PersonId, s);
				}
			}

			Console.WriteLine("  Start training to Person Group");
			await faceServiceClient.TrainPersonGroupAsync(personGroupId);
			TrainingStatus trainingStatus = null;
			while (true)
			{
				trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

				if (trainingStatus.Status != Status.Running)
				{
					break;
				}

				await Task.Delay(1000);
			}
		}
	}
}
