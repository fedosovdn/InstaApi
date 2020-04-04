using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Informers;
using Insta;
using Insta.StoryProcessor;
using Insta.StoryProcessor.Services;
using Insta.UserProcessor.Services;

namespace InstaApiDeveloping
{
    public class JustForTestApi
    {
        public async Task<bool> TestingFunctionalityAsync()
        {
            const string stateFile = "state.bin";
            var apiKeeper = new ApiKeeper();

            var IOService = new ConsoleInputOutputService();

            var resultAuthentificate = await apiKeeper.AuthentificateByDefaultWay(stateFile, IOService);

            if (!resultAuthentificate)
            {
                return false;
            }

            await IOService.OutputMessageAsync("User name to watch stories:");
            var userName = await IOService.GetMessage();

            var userService = new UserService(apiKeeper.InstaApi);
            var userInfo = userService.GetUserInfo(userName).Result;

            var mediaService = new StoryService(apiKeeper.InstaApi);
            var stories = mediaService.GetUserStories(userInfo).Result;

            OpenStoriesInBrowser(stories);

            return true;
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