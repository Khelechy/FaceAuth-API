using FaceAuth.Data;
using FaceAuth.Models;
using FaceAuth.ResponseModels;
using FaceAuth.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceAuth.Services
{
    public interface IAzureServices
    {
        Task<Tuple<bool, string, User>> AddPerson(AddPersonViewModel model);
        Task<Guid> Recognize(RecognizePersonViewModel model);
        Task<IList<DetectedFace>> Detect(IFaceClient faceClient, string image, string imageName, string recognition_model);
    }

    public class AzureServices : IAzureServices
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly FaceAuthContext _context;
        private FaceClient faceClient;

        public AzureServices(IConfiguration configuration, IWebHostEnvironment hostEnvironment, FaceAuthContext context)
        {
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
            _context = context;
            InitFaceClient();
        }

        public async Task<Tuple<bool, string, User>> AddPerson(AddPersonViewModel model)
        {
            try
            {
                string error = "";
                bool isError = false;
                //var personGroupId = _configuration["AzureDetails:PersonGroupID"];
                var personGroupId = model.GroupId;
                var personGroup = await GetLargePersonGroup(personGroupId);
                var recognitionModel = _configuration["AzureDetails:RecognitionModel"];
                if (personGroup == null)
                {
                    //Create new Large person group if it doesn't exist 
                    await faceClient.LargePersonGroup.CreateAsync(personGroupId, "TestGroupKelechiNew", recognitionModel);
                }

                var personGuid = await Recognize(new RecognizePersonViewModel { GroupId = personGroupId, Image = model.Image });
                if (personGuid != null)
                {
                    error = "This face already exists in the database.";
                    isError = true;
                    return new Tuple<bool, string, User>(isError, error, null);
                }

                //Create person
                var person = await faceClient.LargePersonGroupPerson.CreateAsync(personGroupId, model.Email, $"{model.FirstName + " " + model.LastName}|{model.Email}");
                if (person == null)
                {
                    //return error

                    error = "Unable to create user profile. Try Again";
                    isError = true;
                    return new Tuple<bool, string, User>(isError, error, null);
                }

                byte[] bytes = Convert.FromBase64String(model.Image);
                MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
                System.Drawing.Image image = System.Drawing.Image.FromStream(stream, true);
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string path = Path.Combine(wwwRootPath, $"images/{Guid.NewGuid().ToString() + ".png"}");
                image.Save(path);
                var imageStream = new FileStream(path, FileMode.Open);

                PersistedFace face = await faceClient.LargePersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, imageStream);

                var user = new User
                {
                    Id = person.PersonId,
                    Name = model.FirstName + " " + model.LastName,
                    FirstName = model.FirstName,
                    Email = model.Email,
                    LastName = model.LastName,
                    UserGroupId = personGroupId,
                    Role = model.Role,
                    UserData = $"{person.PersonId}|{model.FirstName + " " + model.LastName}|{model.Email}",
                };

                _context.Users.Add(user);
                var result = await _context.SaveChangesAsync();

                if (result < 0)
                {
                    error = "Unable to create user profile. Try Again";
                    isError = true;
                    user = null;
                }
                var isTrained = await TrainGroup(personGroupId);
                return new Tuple<bool, string, User>(isError, error, user);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                var error = ex.Message.ToString();
                return new Tuple<bool, string, User>(true, error, null);
            }

        }

        public async Task<bool> TrainGroup(string largePersonGroupId)
        {
            try
            {
                await faceClient.LargePersonGroup.TrainAsync(largePersonGroupId);

                // Wait until the training is completed.
                while (true)
                {
                    await Task.Delay(1000);
                    var trainingStatus = await faceClient.LargePersonGroup.GetTrainingStatusAsync(largePersonGroupId);
                    if (trainingStatus.Status == TrainingStatusType.Succeeded)
                    {
                        return true;
                    }
                    else if (trainingStatus.Status == TrainingStatusType.Failed)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public async Task<IList<DetectedFace>> Detect(IFaceClient faceClient, string imageString, string imageName, string recognition_model)
        {
            try
            {
                List<DetectedFace> detectedFaces = new List<DetectedFace>();


                byte[] bytes = Convert.FromBase64String(imageString);
                MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
                System.Drawing.Image image = System.Drawing.Image.FromStream(stream, true);
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string path = Path.Combine(wwwRootPath, $"images/{Guid.NewGuid().ToString() + ".png"}");
                image.Save(path);
                var imageStream = new FileStream(path, FileMode.Open);
                var detectedFace = await faceClient.Face.DetectWithStreamAsync(imageStream);
                if (detectedFace.Count < 1)
                    return null;
                else
                    foreach (var face in detectedFace)
                    {
                        detectedFaces.Add(face);
                    }
                return detectedFaces;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<Guid> Recognize(RecognizePersonViewModel model)
        {
            try
            {
                Guid finalPersonId = Guid.Empty;
                //var personGroupId = _configuration["AzureDetails:PersonGroupID"];
                var personGroupId = model.GroupId;
                var recognitionModel = _configuration["AzureDetails:RecognitionModel"];
                var personGroup = await faceClient.LargePersonGroup.GetAsync(personGroupId);
                IList<Guid> sourceFaceIds = new List<Guid>();

                var detectedFaces = await Detect(faceClient, model.Image, Guid.NewGuid().ToString(), recognitionModel);
                if (detectedFaces == null)
                {
                    return Guid.Empty;
                }
                foreach (var detectedFace in detectedFaces)
                {
                    sourceFaceIds.Add(detectedFace.FaceId.Value);
                }

                //var identifyPersons = await faceClient.Face.IdentifyAsync(sourceFaceIds, largePersonGroupId: personGroupId);
                var obj = new
                {
                    faceIds = sourceFaceIds.ToArray(),
                    largePersonGroupId = personGroupId
                };

                var identifyPersons = await IdentifyFace(obj);

                if (identifyPersons == null)
                {
                    return Guid.Empty;
                }
                foreach (var identifyPerson in identifyPersons)
                {
                    if (identifyPerson.Candidates.Count > 0)
                    {
                        Person person = await faceClient.LargePersonGroupPerson.GetAsync(personGroupId, identifyPerson.Candidates[0].PersonId);
                        var confidence = identifyPerson.Candidates[0].Confidence;
                        if (confidence >= 0.5)
                        {
                            finalPersonId = person.PersonId;
                        }
                    }
                }
                return finalPersonId;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Guid.Empty;
            }
        }

        void InitFaceClient()
        {
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(_configuration["AzureDetails:Key"]);
            faceClient = new FaceClient(credentials);
            faceClient.Endpoint = _configuration["AzureDetails:Endpoint"];
            FaceOperations faceOperations = new FaceOperations(faceClient);
        }

        public async Task<PersonGroup> GetLargePersonGroup(string largePersonGroupId)
        {
            try
            {
                var client = new RestClient(_configuration["AzureDetails:Endpoint"]);
                var request = new RestRequest("face/v1.0/largepersongroups/" + largePersonGroupId, Method.GET);
                request.AddHeader("Ocp-Apim-Subscription-Key", _configuration["AzureDetails:Key"]);
                var response = await client.ExecuteAsync<PersonGroup>(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return response.Data;
                else
                    return null;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;

            }
        }

        public async Task<IList<IdentifyResult>> IdentifyFace(object requestObj)
        {
            try
            {
                var client = new RestClient(_configuration["AzureDetails:Endpoint"]);
                var request = new RestRequest("face/v1.0/identify", Method.POST);
                request.AddHeader("Ocp-Apim-Subscription-Key", _configuration["AzureDetails:Key"]);
                var requestBody = JsonConvert.SerializeObject(requestObj);
                request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
                var response = await client.ExecuteAsync<IList<IdentifyResult>>(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return response.Data;
                else
                    return null;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;

            }
        }
    }
}
