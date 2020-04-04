using System.Collections.Generic;

namespace Insta.StoryProcessor
{
    public class StoryInfo
    {
        public string CaptionText { get; set; }

        public IReadOnlyList<ImageInfo> Images { get; set; }

        public IReadOnlyList<VideoInfo> Videos { get; set; }
    }
}
