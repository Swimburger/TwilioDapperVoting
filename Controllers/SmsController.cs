using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.Security;
using Twilio.TwiML;
using TwilioDapperVoting.Data;

namespace TwilioDapperVoting.Controllers
{
    public class SmsController : TwilioController
    {
        private const string NextActionUrlKey = "NextAction";
        private readonly IConfiguration configuration;
        private readonly ElectionRepository electionRepository;

        public SmsController(IConfiguration configuration, ElectionRepository electionRepository)
        {
            this.configuration = configuration;
            this.electionRepository = electionRepository;
        }

        [Route("sms")]
        public IActionResult Index(SmsRequest request)
        {
            var messagingResponse = new MessagingResponse();

            var nextAction = Request.Cookies[NextActionUrlKey];
            if (!string.IsNullOrEmpty(nextAction))
            {
                messagingResponse.Redirect(new Uri(nextAction, UriKind.RelativeOrAbsolute));
                return TwiML(messagingResponse);
            }

            var introSentKey = "intro-sent";
            if (Request.Cookies[introSentKey] != "true")
            {
                messagingResponse.Message("Welcome to the emoji election 🗳");
                Response.Cookies.Append(introSentKey, "true");
            }

            var message = request.Body.Trim().ToLower();
            switch (message)
            {
                case "vote":
                    StartVote(messagingResponse);
                    break;
                case "show results":
                case "results":
                    ShowResults(messagingResponse);
                    break;
                default:
                    messagingResponse.Message("Please respond with \"vote\" or \"show results\"");
                    break;

            }

            return TwiML(messagingResponse);
        }

        [Route("vote")]
        public IActionResult Vote(SmsRequest request)
        {
            var message = request.Body.Trim().ToLower();
            var messagingResponse = new MessagingResponse();
            if (IsSingleEmoji(message))
            {
                electionRepository.Vote(message);
                messagingResponse.Message($"You voted for {message}");
                ShowResults(messagingResponse);
                Response.Cookies.Delete(NextActionUrlKey);
            }
            else if (message == "go back")
            {
                messagingResponse.Redirect(new Uri("./sms", UriKind.Relative));
                Response.Cookies.Delete(NextActionUrlKey);
            }
            else
            {
                messagingResponse.Message("Invalid vote. Respond with a single emoji to vote");
                messagingResponse.Message("Respond with \"go back\" to stop voting");
            }

            return TwiML(messagingResponse);
        }

        public bool IsSingleEmoji(string input)
        {
            // the first emoji in 'input' will be removed, subsequent emoji's will not be removed
            input = new Regex("(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])")  // regex matches emoji's
                .Replace(input, string.Empty, count: 1);
            // if 'input' is empty after the first emoji was removed, 'input' was a single emoji
            var isSingleEmoji = input == string.Empty;
            return isSingleEmoji;
        }

        private void StartVote(MessagingResponse response)
        {
            response.Message("Respond with an emoji to vote");
            Response.Cookies.Append(NextActionUrlKey, "./vote");
        }

        private void ShowResults(MessagingResponse response)
        {
            Dictionary<string, int> emojiTally = electionRepository.GetEmojiTally();
            if (emojiTally.Count == 0)
            {
                response.Message("No votes are in yet 😲");
            }
            else
            {
                var message = "The results are\n";
                message += string.Join("\n", emojiTally
                    .OrderByDescending(pair => pair.Value)
                    .Select(pair => $"{pair.Key}: {pair.Value} votes"));
                response.Message(message);
            }
        }
    }
}