using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using System.Threading.Tasks;

namespace Insta.UserProcessor.Services
{
    public class UserService
    {
        private IInstaApi _instaApi;

        public UserService(IInstaApi instaApi)
        {
            _instaApi = instaApi;
        }

        public async Task<InstaUserInfo> GetUserInfo(string userName)
        {
            //обработать случай что по имени не нашелся юзер
            return (await _instaApi.UserProcessor
                .GetUserInfoByUsernameAsync(userName)).Value;
        }
    }
}
