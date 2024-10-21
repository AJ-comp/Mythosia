using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Azure.Storage.Blobs
{
    public class ExtendBlobServiceClient : BlobServiceClient
    {
        // Key Vault에서 비밀을 사용하여 BlobServiceClient 생성자를 호출하는 생성자
        public ExtendBlobServiceClient(string keyVaultUrl, string secretName)
            : base(GetConnectionStringFromKeyVault(keyVaultUrl, secretName))
        {
        }

        // 기본 connectionString을 사용하여 생성하는 생성자
        public ExtendBlobServiceClient(string connectionString) : base(connectionString)
        {
        }

        // Key Vault에서 비밀을 가져와서 connectionString을 반환하는 메서드
        private static string GetConnectionStringFromKeyVault(string keyVaultUrl, string secretName)
        {
            // DefaultAzureCredential을 사용하면 Managed Identity를 통해 자동으로 인증이 이루어짐
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            // 비밀 값 가져오기 (비동기 작업을 동기적으로 기다림)
            KeyVaultSecret secret = client.GetSecretAsync(secretName).Result;

            // connectionString 반환
            return secret.Value;
        }

        public string GenerateSasTokenUri(string containerName, string blobName, BlobSasPermissions permissions, DateTimeOffset tokenTimeout)
        {
            var containerClient = GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = tokenTimeout   // 토큰 만료 시간 설정
            };

            // 필요한 권한 설정
            sasBuilder.SetPermissions(permissions);

            // SAS URI 생성
            //            var sasToken = blobClient.GenerateSasUri(sasBuilder).Query;

            return blobClient.GenerateSasUri(sasBuilder).AbsoluteUri;
        }


        public async Task UploadAsync(string containerName, string blobName, Stream fileStream)
        {
            // create BlobContainerClient
            BlobContainerClient containerClient = GetBlobContainerClient(containerName);

            // create the container if it is not exist
            await containerClient.CreateIfNotExistsAsync();

            // create BlobClient
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // upload binary data to Blob
            await blobClient.UploadAsync(fileStream, overwrite: true);
        }


        public async Task UploadAsync(string containerName, string blobName, string content)
        {
            await UploadAsync(containerName, blobName, new MemoryStream(content.ToUTF8Array()));
        }


        // IFormFile 업로드 메서드 (IFormFile을 스트림으로 변환하여 호출)
        public async Task UploadAsync(string containerName, string blobName, IFormFile file)
        {
            using (Stream fileStream = file.OpenReadStream())
            {
                await UploadAsync(containerName, blobName, fileStream);
            }
        }


        // 실제 Append 로직을 담당하는 공통 메서드
        private async Task AppendAsync(string containerName, string blobName, Stream stream)
        {
            var containerClient = GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var appendBlobClient = containerClient.GetAppendBlobClient(blobName);

            try
            {
                // Blob이 존재하지 않으면 생성
                await appendBlobClient.CreateIfNotExistsAsync();
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "BlobAlreadyExists")
            {
                // Blob가 이미 존재하면 무시
            }

            // 스트림 내용을 blob의 끝에 추가
            await appendBlobClient.AppendBlockAsync(stream);
        }


        // 기본적으로 문자열을 처리하는 AppendAsync
        public async Task AppendAsync(string containerName, string blobName, string content)
        {
            await AppendAsync(containerName, blobName, new MemoryStream(content.ToUTF8Array()));
        }

        // IFormFile을 처리하는 AppendAsync
        public async Task AppendAsync(string containerName, string blobName, IFormFile file)
        {
            using var stream = file.OpenReadStream();
            await AppendAsync(containerName, blobName, stream);
        }



        public async Task<Stream> DownloadAsync(string containerName, string blobName)
        {
            BlobContainerClient containerClient = GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobDownloadInfo download = await blobClient.DownloadAsync();
            MemoryStream memoryStream = new MemoryStream();
            await download.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // 스트림 포인터를 처음으로 돌립니다.

            return memoryStream; // 스트림 반환
        }


        public async Task<string> DownloadAsStringAsync(string containerName, string blobName)
        {
            BlobContainerClient containerClient = GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobDownloadInfo download = await blobClient.DownloadAsync();
            using (StreamReader reader = new StreamReader(download.Content, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync(); // Blob 파일을 문자열로 변환 후 반환
            }
        }

        public async Task DownloadToFileAsync(string containerName, string blobName, string filePath)
        {
            BlobContainerClient containerClient = GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobDownloadInfo download = await blobClient.DownloadAsync();
            using (var fileStream = File.OpenWrite(filePath))
            {
                await download.Content.CopyToAsync(fileStream); // 로컬 파일로 저장
            }
        }
    }
}
