// -----------------------------------------------------------------------
//  <copyright file="TrackManager.cs" company="Henric Jungheim">
//  Copyright (c) 2012-2015.
//  <author>Henric Jungheim</author>
//  </copyright>
// -----------------------------------------------------------------------
// Copyright (c) 2012-2015 Henric Jungheim <software@henric.org>
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using SM.Media.Playlists;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SM.Media.BackgroundAudio
{
    static class TrackManager
    {
        static MediaTrack[] Sources =
        {
            //new MediaTrack {
            //    Title = "ABC",
            //    Url = new Uri("http://livestream01.mcgi.org:1935/public/mcgicp-a/playlist.m3u8")
            //}
            //new MediaTrack
            //{
            //    Title = "NASA TV",
            //    Url = new Uri("http://www.nasa.gov/multimedia/nasatv/NTV-Public-IPS.m3u8")
            //},
            //new MediaTrack
            //{
            //    Title = "NPR",
            //    Url = new Uri("http://www.npr.org/streams/mp3/nprlive24.pls")
            //},
            //new MediaTrack
            //{
            //    Title = "Bjarne Stroustrup - The Essence of C++",
            //    Url = new Uri("http://media.ch9.ms/ch9/ca9a/66ac2da7-efca-4e13-a494-62843281ca9a/GN13BjarneStroustrup.mp3"),
            //    UseNativePlayer = true
            //},
            //null,
            //new MediaTrack
            //{
            //    Title = "Apple",
            //    Url = new Uri("http://devimages.apple.com/iphone/samples/bipbop/bipbopall.m3u8")
            //}
        };

        public static IList<MediaTrack> Tracks
        {
            get {
                return Sources;
            }
        }

        public static async void GetTracks()
        {
            MediaTrack md = await GetLink();
            MediaTrack[] sources = {
                new MediaTrack() {
                    Title = md.Title,
                    Url = md.Url
                }
            };

            Sources = sources;
        }



        private const string CP_AUDIO = "http://www.mcgi.org/api/get_post/?callback=?&post_type=Streaming&post_id=2453&dev=1";

        /// <summary>
        /// Gets the Links' details.
        /// </summary>
        /// <returns>Broadcast links details.</returns>
        public static async Task<MediaTrack> GetLink()
        {
            MediaTrack _myLinks = null;

            // Retrieve Links' details
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync(CP_AUDIO);
                response.EnsureSuccessStatusCode();

                var jsonResult = await response.Content.ReadAsStringAsync();

                var cp = JsonConvert.DeserializeObject<CommunityPrayerModel>(jsonResult);

                _myLinks = new MediaTrack() {
                    Title = "MCGI - COMMUNITY PRAYER",
                    Url = new Uri(cp.Post.URL)
                };
            }
            catch (Exception)
            {

            }

            return _myLinks;
        }
    }
    
    
    /// <summary>
    /// Model for Community Prayer Links
    /// </summary>
    public sealed class CommunityPrayerModel
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("post")]
        public Post Post { get; set; }
    }

    public sealed class Post
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
