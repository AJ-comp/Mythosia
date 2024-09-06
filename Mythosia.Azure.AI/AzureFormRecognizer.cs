using Mythosia.Azure;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Mythosia
{
    internal class AzureFormRecognizer
    {
        public string EndPoint { get; private set; } = string.Empty;
        public string Key { get; private set; } = string.Empty;


        public AzureFormRecognizer(SecretFetcher endPointSecret, SecretFetcher keySecret)
        {
            EndPoint = endPointSecret.GetKeyValueAsync().Result;
            Key = keySecret.GetKeyValueAsync().Result;
        }


        public AzureFormRecognizer(string endPoint, string key)
        {
            EndPoint = endPoint;
            Key = key;
        }


        public async Task<string> ExtractContentFromDocumentAsync(string filePath)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Key);

            var url = $"{EndPoint}/formrecognizer/documentModels/prebuilt-layout:analyze?api-version=2022-08-31";
            using var content = new StreamContent(System.IO.File.OpenRead(filePath));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            var response = await client.PostAsync(url, content);
            // 응답 상태 코드 확인
            Console.WriteLine($"Status Code: {response.StatusCode}");

            // 작업 위치 확인
            if (response.StatusCode == HttpStatusCode.Accepted && response.Headers.Contains("operation-location"))
            {
                var operationLocation = response.Headers.GetValues("operation-location").FirstOrDefault();
                Console.WriteLine($"Operation Location: {operationLocation}");

                // 비동기 작업의 결과를 확인
                return await CheckOperationStatusAsync(client, operationLocation);
            }

            return string.Empty;
        }


        private static async Task<string> CheckOperationStatusAsync(HttpClient client, string operationLocation)
        {
            string result = string.Empty;
            while (true)
            {
                var response = await client.GetAsync(operationLocation);
                string content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {content}");

                // JSON 파싱을 통해 작업 상태 확인
                var jsonObject = JObject.Parse(content);
                var status = jsonObject["status"]?.ToString();

                // 작업 상태에 따른 처리
                if (status == "succeeded")
                {
                    // 작업이 성공적으로 완료된 경우 루프를 빠져나갑니다.
                    result = content;
                    break;
                }
                else if (status == "failed")
                {
                    // 작업이 실패한 경우 루프를 종료하고 결과를 반환합니다.
                    Console.WriteLine("The operation failed.");
                    break;
                }
                else if (status == "running")
                {
                    // 작업이 진행 중인 경우 일정 시간 대기 후 다시 상태를 확인합니다.
                    Console.WriteLine("The operation is still running. Waiting for completion...");
                }
                else
                {
                    // 예기치 않은 상태가 발생한 경우, 루프를 종료합니다.
                    Console.WriteLine("Unexpected status encountered.");
                    break;
                }

                // 일정 시간 대기 후 재시도
                await Task.Delay(2000);
            }

            return result;
        }
    }

    /*
    private static void ConvertJsonToJsonl(string jsonContent, string outputPath)
    {
        // JSON을 객체로 파싱
        var jsonObject = JToken.Parse(jsonContent);

        using (var file = new StreamWriter(outputPath))
        {
            // JSON 데이터가 배열일 경우
            if (jsonObject is JArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    var jsonLine = item.ToString(Newtonsoft.Json.Formatting.Indented);
                    file.WriteLine(jsonLine);
                }
            }
            // JSON 데이터가 객체일 경우
            else if (jsonObject is JObject)
            {
                var jsonLine = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
                file.WriteLine(jsonLine);
            }
            else
            {
                Console.WriteLine("Unrecognized JSON structure. Only JSON objects and arrays are supported.");
            }
        }

        Console.WriteLine("JSONL file created successfully.");
    }
    */
}
