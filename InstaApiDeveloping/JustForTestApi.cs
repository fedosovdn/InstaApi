using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Informers;
using Insta;
using Insta.AuthenticationProcessor.Services;
using Insta.StoryProcessor;
using Insta.StoryProcessor.Services;
using Insta.UserProcessor.Services;

namespace InstaApiDeveloping
{
    public class JustForTestApi
    {
        private const string SymbolToExit = "q";

        public async Task<bool> TestingFunctionalityAsync()
        {
            const string stateFile = "state.bin";

            var IOService = new ConsoleInputOutputService();

            var authentificator = new Authentificator();
            var (resultAuthentificate, instaApi) = await authentificator.AuthentificateByDefaultWay(stateFile, IOService);

            if (!resultAuthentificate)
            {
                return false;
            }

            var apiKeeper = new ApiKeeper(instaApi);

            ShowStories(apiKeeper, IOService);

            return true;
        }

        private async void ShowStories(ApiKeeper apiKeeper, IInputOutputService IOService)
        {
            var exitFlag = false;

            do
            {
                await IOService.OutputMessageAsync($"User name to watch stories (press {SymbolToExit} to exit):");
                var userName = await IOService.GetMessage();

                if (userName == SymbolToExit)
                {
                    exitFlag = true;
                }
                else
                {
                    var userService = new UserService(apiKeeper.InstaApi);
                    var userInfo = userService.GetUserInfo(userName).Result;

                    var mediaService = new StoryService(apiKeeper.InstaApi);
                    var stories = mediaService.GetUserStories(userInfo).Result;

                    OpenStoriesInBrowser(stories);
                }
            } while (!exitFlag);
        }

        private void OpenStoriesInBrowser(IReadOnlyList<StoryInfo> stories)
        {
            var uris = stories.SelectMany(st =>
                st.Images.Select(img => img.Uri).Distinct().Concat(
                st.Videos.Select(vd => vd.Uri).Distinct()
                )).ToArray();

            Process myProcess = new Process();

            foreach (var uri in uris)
            {
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = uri;
                myProcess.Start();
            }
        }
    }
}