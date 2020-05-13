//Ссылку на информеров надо будет через nuget

namespace Insta
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using InstagramApiSharp.API;

    public class ApiKeeper
    {
        public IInstaApi InstaApi { get; set; }

        public ApiKeeper(IInstaApi instaApi)
        {
            InstaApi = instaApi;
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
    }
}