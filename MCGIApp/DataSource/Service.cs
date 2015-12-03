using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MCGIApp.DataSource
{
    public class Service
    {
        private const string CP_VIDEO = "http://www.mcgi.org/api/get_post/?callback=?&post_type=Streaming&post_id=2452&dev=1";
        private const string CP_AUDIO = "http://www.mcgi.org/api/get_post/?callback=?&post_type=Streaming&post_id=2453&dev=1";
        CommunityPrayerModel _videoLink = new CommunityPrayerModel();
        CommunityPrayerModel _audioLink = new CommunityPrayerModel();

        /// <summary>
        /// Gets the Links' details.
        /// </summary>
        /// <returns>Broadcast links details.</returns>
        private async Task<CommunityPrayerModel> GetLink(string link)
        {
            CommunityPrayerModel _myLinks = new CommunityPrayerModel();

            // Retrieve Links' details
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync(link);
                response.EnsureSuccessStatusCode();

                var jsonResult = await response.Content.ReadAsStringAsync();

                _myLinks = JsonConvert.DeserializeObject<CommunityPrayerModel>(jsonResult);
            }
            catch (Exception)
            {

            }

            return _myLinks;
        }

        public async Task<Post> GetVideoLink()
        {
            if (_videoLink.Post != null)
            {
                return _videoLink.Post;
            }
            _videoLink = await this.GetLink(CP_VIDEO);

            if (_videoLink.Post != null)
            {
                _videoLink.Post.Title = "Video";
                _videoLink.Post.ImagePath = "/Assets/Icons/icon-video.png";
            }

            return _videoLink.Post;
        }

        public async Task<Post> GetAudioLink()
        {
            if (_audioLink.Post != null)
            {
                return _audioLink.Post;
            }
            _audioLink = await this.GetLink(CP_AUDIO);

            if (_audioLink.Post != null)
            {
                _audioLink.Post.Title = "Audio";
                _audioLink.Post.ImagePath = "/Assets/Icons/icon-audio.png";
            }

            return _audioLink.Post;
        }
    }

    /// <summary>
    /// Model for Community Prayer Links
    /// </summary>
    public class CommunityPrayerModel
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("post")]
        public Post Post { get; set; }
    }

    public class Post
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("excerpt")]
        public string URL { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("date")]
        public string Date { get; set; }
        public string ImagePath { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        public bool Visibility { get; set; }
    }
}
