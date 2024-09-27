﻿using Mythosia.Azure;

namespace Mythosia.AI.Tests
{
    [TestClass()]
    public class ChatGptServiceTests
    {
        [TestMethod()]
        public async Task ChatGptServiceTest()
        {
            try
            {
                SecretFetcher secretFetcher = new SecretFetcher("https://mythosia-key-vault.vault.azure.net/", "momedit-openai-secret");
                var chatGptService = new ChatGptService(await secretFetcher.GetKeyValueAsync(), new HttpClient());
                chatGptService.ActivateChat.SystemMessage = "귀여운 어투로 말해주세요";

                var result = await chatGptService.GetCompletionAsync("안녕하세요 당신에 대해 소개해주세요");
                await chatGptService.StreamCompletionAsync("당신의 정적인 취미와 동적인 취미에 대해 말해줘요", (message) => { Console.WriteLine(message); });
                await chatGptService.StreamCompletionAsync("그 둘 중 무엇을 더 좋아하나요?", async (message) => { Console.WriteLine(message); });
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
        }
    }
}