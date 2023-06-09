﻿using LineService.Data;
using LineService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static LineService.Models.LineMessage;

namespace LineService.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LineServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string lineUrl = "https://api.line.me/v2/bot";
        //private readonly string tokenLine = "V9+j5TbDKQGcF/RCRHhkUn2fyufSuxtShgqLKz4n+8VPT2FFaKCk0uCN6ZFxDzvnG455liuKd/U4IphqLg6PNl2E7A0IYSfCg787Irx07uVc4KCZRInS0gaV1FbMJBePSVs4AYS9AN6hJppNM8946gdB04t89/1O/w1cDnyilFU=";
        private readonly string tokenLine = "8jcva04mhIBVnMFqCfaQe5NNIkpAA5xm/uq2IOPRpGzHcSCCYQV+FbAhqI06FxzPorWCpL6tXSFcSJBHp0OESgaoomYPFofuQ71uO5Q0NzmBiMsVkBnIgeArfezFw+dgJRgAvzHPN054amxDLexEcAdB04t89/1O/w1cDnyilFU=";
        public LineServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] dynamic request)
        {
            try
            {
                var httpClient = new HttpClient();
                var json = Convert.ToString(request);
                JObject data = JObject.Parse(json);
                var eventType = data["events"][0]["type"].ToString();
                var userId = data["events"][0]["source"]["userId"].ToString();
                if (eventType == "follow")
                {
                    var botUrl = $"{lineUrl}/profile/{userId}";
                    var req = new HttpRequestMessage(HttpMethod.Get, botUrl);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenLine);
                    var response = await httpClient.SendAsync(req);
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResult = await response.Content.ReadAsStringAsync();
                        var user = JsonConvert.DeserializeObject<LineProfile>(jsonResult);
                        if (user != null)
                        {
                            await _context.LineProfile.AddAsync(user);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
            return Ok();
        }

        [HttpPost]
        [Route("SendMultiMessage")]
        public async Task<IActionResult> SendMultiMessage([FromBody] SendMultiMessage request)
        {
            
            
            var httpClient = new HttpClient();
            var users = request.listName.Split("\n").ToList();
            List<string> userIdList = new List<string>();
            var listUser = _context.LineProfile.Where(x=>x.FullName != null).ToListAsync().Result;
            foreach (var user in users)
            {
                var fullName = user.Replace(" ", "");
                var checkUser = listUser.SingleOrDefault(x => x.FullName.Replace(" ", "") == fullName);
                if (checkUser != null)
                {
                    userIdList.Add(checkUser.userId);
                }
            }
            Console.WriteLine(true);
            if (userIdList.Count > 0)
            {
                var uri = $"{lineUrl}/message/multicast";
                using (var req = new HttpRequestMessage(HttpMethod.Post, uri))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenLine);
                    var content = new PushLineMsg
                    {
                        to = userIdList,
                        messages = new List<TextMessage>
                    {
                        new TextMessage
                        {
                            text = request.Message,
                            type = "text"
                        }
                    }
                    };
                    req.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                    var data = await httpClient.SendAsync(req);
                }
            }
            return Ok();
        }

        [HttpPost]
        [Route("SendImageMessage")]
        public void LineNotify(string lineToken, string text, string pictureUrl)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string message = System.Web.HttpUtility.UrlEncode(text, Encoding.UTF8);
                var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                var postData = string.Format("message={0}", message);
                if (pictureUrl != "")
                {
                    var imageThumbnail = string.Format("imageThumbnail={0}", pictureUrl);
                    var imageFullsize = string.Format("imageFullsize={0}", pictureUrl);
                    postData += "&" + imageThumbnail.ToString() + "&" + imageFullsize.ToString();
                }
                var data = Encoding.UTF8.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Headers.Add("Authorization", "Bearer " + lineToken);
                var stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        public IActionResult TestLineMessage()
        {
            var client = new RestClient("https://api.line.me/v2/bot/message/push");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer 8jcva04mhIBVnMFqCfaQe5NNIkpAA5xm/uq2IOPRpGzHcSCCYQV+FbAhqI06FxzPorWCpL6tXSFcSJBHp0OESgaoomYPFofuQ71uO5Q0NzmBiMsVkBnIgeArfezFw+dgJRgAvzHPN054amxDLexEcAdB04t89/1O/w1cDnyilFU=");
            request.AddHeader("Content-Type", "application/json");
            var body = @"{
" + "\n" +
            @"    ""to"": ""Uc993a7060da875cc7c48196773ff4175"",
" + "\n" +
            @"    ""messages"":[
" + "\n" +
            @"        {
" + "\n" +
            @"            ""type"":""text"",
" + "\n" +
            @"            ""text"":""Hello, world1""
" + "\n" +
            @"        },
" + "\n" +
            @"        {
" + "\n" +
            @"            ""type"":""text"",
" + "\n" +
            @"            ""text"":""Hello, world2""
" + "\n" +
            @"        }
" + "\n" +
            @"    ]
" + "\n" +
            @"}";
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);

            //return RedirectToAction("BoardCast");
            return Ok();
        }
        //socket connection

    }
}
