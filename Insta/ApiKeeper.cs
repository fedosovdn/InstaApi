//Ссылку на информеров надо будет через nuget

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Informers;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;

namespace Insta
{
    public interface IApiKeeper
    {
    }

    public class ApiKeeper : IApiKeeper
    {
        public IInstaApi InstaApi { get; set; }

        //не рекомендуется, лучше стобы клиент полностью строил архитектуру сам
        public async Task<bool> AuthentificateByDefaultWay(string stateFile, IInputOutputService IOService)
        {
            var authentificateResult = await AuthentificateFromStateFileAsync(stateFile, IOService);

            var agreementAnswer = EAgreementAnswer.No;
            if (authentificateResult)
            {
                agreementAnswer = await IOService.GetAgreementAnswer($"Do you want to continue as {InstaApi.GetLoggedUser().UserName}");
            }

            if (!authentificateResult || agreementAnswer == EAgreementAnswer.No)
            {
                await IOService.
                 OutputMessageAsync("Enter Your user name:");
                var userName = await IOService.GetMessage();
                await IOService.OutputMessageAsync("Enter Your password:");
                var password = await IOService.GetMessage();

                var userData = new UserSessionData
                {
                    UserName = userName,
                    Password = password
                };

                return await AuthenticateAsync(userData, stateFile, IOService);
            }

            return true;
        }

        public async Task<bool> AuthentificateFromStateFileAsync(string stateFile, IInputOutputService IOService)
        {
            try
            {
                // load session file if exists
                if (File.Exists(stateFile))
                {
                    await IOService.OutputMessageAsync("Loading state from file");

                    InstaApi = InstaApiBuilder.CreateBuilder()
                        .SetUser(InstagramApiSharp.Classes.UserSessionData.Empty)
                        .Build();

                    using (var fs = File.OpenRead(stateFile))
                    {
                        await InstaApi.LoadStateDataFromStreamAsync(fs);
                        // in .net core or uwp apps don't use LoadStateDataFromStream
                        // use this one:
                        // _instaApi.LoadStateDataFromString(new StreamReader(fs).ReadToEnd());
                        // you should pass json string as parameter to this function.
                    }
                }
                else
                {
                    await IOService.OutputMessageAsync($"There is no such file {stateFile}");
                    return false;
                }
            }
            catch (Exception e)
            {
                await IOService.OutputMessageAsync(e.Message);
                return false;
            }

            return InstaApi.IsUserAuthenticated;
        }

        public async Task<bool> AuthenticateAsync(UserSessionData userSessionData, string stateFile, IInputOutputService IOService)
        {
            var userSesData = new InstagramApiSharp.Classes.UserSessionData
            {
                UserName = userSessionData.UserName,
                Password = userSessionData.Password
            };

            InstaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSesData)
                .Build();

            if (!InstaApi.IsUserAuthenticated)
            {
                // login
                await IOService.OutputMessageAsync($"Logging in as {userSessionData.UserName}");
                var logInResult = await InstaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    await IOService.OutputMessageAsync($"Unable to login: {logInResult.Info.Message}");

                    if (logInResult.Value == InstaLoginResult.ChallengeRequired)
                    {
                        var verificationResult = await AuthenticateByVerificationsAsync(userSessionData, IOService);

                        if (!verificationResult)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            SaveSessionToFile(stateFile);

            return true;
        }

        private async Task<bool> AuthenticateByVerificationsAsync(UserSessionData userSessionData, IInputOutputService IOService)
        {
            var challenge = await InstaApi.GetChallengeRequireVerifyMethodAsync();
            if (challenge.Succeeded)
            {
                if (challenge.Value.SubmitPhoneRequired)
                {
                    await IOService.OutputMessageAsync("Please enter your phone number to verify:");
                    var phoneNumber = await IOService.GetMessage();

                    var submitPhone = await InstaApi.SubmitPhoneNumberForChallengeRequireAsync(phoneNumber);
                    if (submitPhone.Succeeded)
                    {
                        await IOService.OutputMessageAsync("Phone number for Challenge submitted");
                    }
                    else
                    {
                        await IOService.OutputMessageAsync(submitPhone.Info.Message);
                    }
                }
                else
                {
                    if (challenge.Value.StepData != null)
                    {
                        if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                        {
                            // send verification code to phone number
                            var phoneNumber = await InstaApi.RequestVerifyCodeToSMSForChallengeRequireAsync();
                            await IOService.OutputMessageAsync($"Enter a code sent to your number: {phoneNumber.Value.StepData.PhoneNumberPreview}");
                        }
                        if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
                        {
                            var email = await InstaApi.RequestVerifyCodeToEmailForChallengeRequireAsync();
                            await IOService.OutputMessageAsync($"Enter a code sent to your mail: {email.Value.StepData.ContactPoint}");
                        }
                    }
                }
            }
            else
            {
                await IOService.OutputMessageAsync(challenge.Info.Message);
            }

            string code = await IOService.GetMessage();
            var verifyLogin = await InstaApi.VerifyCodeForChallengeRequireAsync(code);
            if (verifyLogin.Succeeded)
            {
                if (InstaApi == null || !InstaApi.IsUserAuthenticated)
                {
                    return false;
                }

                return true;
            }
            else
            {
                // two factor is required
                if (verifyLogin.Value == InstaLoginResult.TwoFactorRequired)
                {
                    await IOService.OutputMessageAsync("Two Factor required");
                }
                else
                {
                    await IOService.OutputMessageAsync(verifyLogin.Info.Message);
                }

                return false;
            }
        }

        private void SaveSessionToFile(string stateFile)
        {
            // save session in file
            var state = InstaApi.GetStateDataAsStream();
            // in .net core or uwp apps don't use GetStateDataAsStream.
            // use this one:
            // var state = _instaApi.GetStateDataAsString();
            // this returns you session as json string.
            using (var fileStream = File.Create(stateFile))
            {
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(fileStream);
            }
        }

        public async Task DoShow()
        {
            // search for related locations near location with latitude = 55.753923, logitude = 37.620940
            // additionaly you can specify search query or just empty string
            var result = await InstaApi.LocationProcessor.SearchLocationAsync(55.692293896284056, 37.67332077026368, string.Empty);
            Console.WriteLine($"Loaded {result.Value.Count} locations");
            var firstLocation = result.Value?.FirstOrDefault(x => x.Name == "Остров Мечты");
            if (firstLocation == null)
                return;
            Console.WriteLine($"Loading feed for location: name={firstLocation.Name}; id={firstLocation.ExternalId}.");

            var locationStories =
                await InstaApi.LocationProcessor.GetLocationStoriesAsync(long.Parse(firstLocation.ExternalId));

            var user = locationStories.Value.Items.Where(x => x.User.UserName == "novoselova.al").ToArray();

            Console.WriteLine(locationStories.Succeeded
                ? $"Loaded {locationStories.Value.Items?.Count} stoires for location"
                : $"Unable to load location '{firstLocation.Name}' stories");
        }

        public async Task DoShowStories()
        {
            var result = await InstaApi.StoryProcessor.GetStoryFeedAsync();
            if (!result.Succeeded)
            {
                Console.WriteLine($"Unable to get story feed: {result.Info}");
                return;
            }
            var storyFeed = result.Value;
            Console.WriteLine($"Got {storyFeed.Items.Count} story reels.");
            foreach (var feedItem in storyFeed.Items)
            {
                Console.WriteLine($"User: {feedItem.User.FullName}");
                foreach (var item in feedItem.Items)
                    Console.WriteLine(
                        $"Story item: {item.Caption?.Text ?? item.Code}, images:{item.ImageList?.Count ?? 0}, videos: {item.VideoList?.Count ?? 0}");
            }
        }

        //удалить
        public async Task UserInfo(string userName)
        {
            var userInfo = await InstaApi.UserProcessor
                .GetUserInfoByUsernameAsync(userName);

            var storyUris = (await InstaApi.UserProcessor
                .GetFullUserInfoAsync(userInfo.Value.Pk)
                ).Value.UserStory.Reel.Items
                .SelectMany(storyItem => storyItem.ImageList.Select(img => img.Uri).Union(storyItem.VideoList.Select(vid => vid.Uri)))
                .Distinct().ToArray();

            var mediaUris = (await InstaApi.UserProcessor
                .GetFullUserInfoAsync(userInfo.Value.Pk)
                ).Value.Feed.Items
                .SelectMany(storyItem => storyItem.Images.Select(img => img.Uri).Union(storyItem.Videos.Select(vid => vid.Uri)))
                .Distinct().ToArray();
        }
    }
}