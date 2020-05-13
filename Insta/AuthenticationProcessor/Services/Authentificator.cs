//Ссылку на информеров надо будет через nuget

namespace Insta.AuthenticationProcessor.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Informers;
    using InstagramApiSharp.API;
    using InstagramApiSharp.API.Builder;
    using InstagramApiSharp.Classes;

    public class Authentificator
    {
        public async Task<(bool, IInstaApi)> AuthentificateByDefaultWay(string stateFile, IInputOutputService IOService)
        {
            var (authentificateResult, instaApi) = await AuthentificateFromStateFileAsync(stateFile, IOService);

            var agreementAnswer = EAgreementAnswer.No;
            if (authentificateResult)
            {
                agreementAnswer = await IOService.GetAgreementAnswer($"Do you want to continue as {instaApi.GetLoggedUser().UserName}");
            }

            if (authentificateResult && agreementAnswer == EAgreementAnswer.Yes)
            {
                return (true, instaApi);
            }

            await IOService.OutputMessageAsync("Enter Your user name:");
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

        public async Task<(bool, IInstaApi)> AuthentificateFromStateFileAsync(string stateFile, IInputOutputService IOService)
        {
            try
            {
                // load session file if exists
                if (File.Exists(stateFile))
                {
                    await IOService.OutputMessageAsync("Loading state from file");

                    IInstaApi instaApi = InstaApiBuilder.CreateBuilder()
                        .SetUser(UserSessionData.Empty)
                        .Build();

                    using (var fs = File.OpenRead(stateFile))
                    {
                        await instaApi.LoadStateDataFromStreamAsync(fs);
                        // in .net core or uwp apps don't use LoadStateDataFromStream
                        // use this one:
                        // _instaApi.LoadStateDataFromString(new StreamReader(fs).ReadToEnd());
                        // you should pass json string as parameter to this function.
                    }

                    return (instaApi.IsUserAuthenticated, instaApi);
                }
                else
                {
                    await IOService.OutputMessageAsync($"There is no such file {stateFile}");
                    return (false, null);
                }
            }
            catch (Exception e)
            {
                await IOService.OutputMessageAsync(e.Message);
                return (false, null);
            }
        }

        public async Task<(bool, IInstaApi)> AuthenticateAsync(UserSessionData userSessionData, string stateFile, IInputOutputService IOService)
        {
            var userSesData = new UserSessionData
            {
                UserName = userSessionData.UserName,
                Password = userSessionData.Password
            };

            IInstaApi instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSesData)
                .Build();

            if (!instaApi.IsUserAuthenticated)
            {
                // login
                await IOService.OutputMessageAsync($"Logging in as {userSessionData.UserName}");
                var logInResult = await instaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    await IOService.OutputMessageAsync($"Unable to login: {logInResult.Info.Message}");

                    switch (logInResult.Value)
                    {
                        case InstaLoginResult.ChallengeRequired:
                            var verificationResult = await AuthenticateByVerificationsAsync(userSessionData, IOService, instaApi);
                            if (!verificationResult)
                            {
                                return (false, instaApi);
                            }
                            break;
                        case InstaLoginResult.TwoFactorRequired:
                            var twoFactorLoginResult = await AuthenticateByTwoFactorAsync(userSessionData, IOService, instaApi);
                            if (!twoFactorLoginResult)
                            {
                                return (false, instaApi);
                            }
                            break;
                        default:
                            return (false, instaApi);

                    }
                }
            }

            SaveSessionToFile(stateFile, instaApi);

            return (true, instaApi);
        }

        private async Task<bool> AuthenticateByVerificationsAsync(UserSessionData userSessionData, IInputOutputService IOService, IInstaApi instaApi)
        {
            var challenge = await instaApi.GetChallengeRequireVerifyMethodAsync();
            if (challenge.Succeeded)
            {
                if (challenge.Value.SubmitPhoneRequired)
                {
                    await IOService.OutputMessageAsync("Please enter your phone number to verify:");
                    var phoneNumber = await IOService.GetMessage();

                    var submitPhone = await instaApi.SubmitPhoneNumberForChallengeRequireAsync(phoneNumber);
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
                            var phoneNumber = await instaApi.RequestVerifyCodeToSMSForChallengeRequireAsync();
                            await IOService.OutputMessageAsync($"Enter a code sent to your number: {phoneNumber.Value.StepData.PhoneNumberPreview}");
                        }
                        else if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
                        {
                            var email = await instaApi.RequestVerifyCodeToEmailForChallengeRequireAsync();
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
            var verifyLogin = await instaApi.VerifyCodeForChallengeRequireAsync(code);
            if (verifyLogin.Succeeded)
            {
                if (instaApi == null || !instaApi.IsUserAuthenticated)
                {
                    return false;
                }

                return true;
            }
            else
            {
                await IOService.OutputMessageAsync(verifyLogin.Info.Message);
                return false;
            }
        }

        private async Task<bool> AuthenticateByTwoFactorAsync(UserSessionData userSessionData, IInputOutputService IOService, IInstaApi instaApi)
        {
            await IOService.OutputMessageAsync("Enter the code:");
            var twoFactorCode = await IOService.GetMessage();

            var twoFactorLoginResult = await instaApi.TwoFactorLoginAsync(twoFactorCode);

            if (twoFactorLoginResult.Succeeded)
            {
                return true;
            }

            return await AuthenticateByVerificationsAsync(userSessionData, IOService, instaApi);
        }

        private void SaveSessionToFile(string stateFile, IInstaApi instaApi)
        {
            // save session in file
            var state = instaApi.GetStateDataAsStream();
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
    }
}
