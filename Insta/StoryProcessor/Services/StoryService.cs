using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Insta.StoryProcessor.Services
{
    public class StoryService
    {
        private IInstaApi _instaApi;

        public StoryService(IInstaApi instaApi)
        {
            _instaApi = instaApi;
        }

        //нужно переделать возвращаемый тип (bool Success, IReadOnlyList<IMediaInfo> Stoies)
        public async Task<IReadOnlyList<StoryInfo>> GetUserStories(InstaUserInfo instaUserInfo)
        {
            var storyFeed = await _instaApi.StoryProcessor.GetUserStoryFeedAsync(instaUserInfo.Pk);

            return storyFeed.Value.Items.Select(story => new StoryInfo
            {
                CaptionText = story.Caption?.Text,
                Images = story.ImageList.Select(img => new ImageInfo { Uri = img.Uri }).ToArray(),
                Videos = story.VideoList.Select(vd => new VideoInfo { Uri = vd.Uri }).ToArray()
            }).ToArray();
        }
    }
}
