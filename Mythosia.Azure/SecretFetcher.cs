using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mythosia.Azure
{
    public class SecretFetcher
    {
        public string KeyVaultUriString { get; set; } = string.Empty;
        public string SecretName { get; set; } = string.Empty;

        public Uri KeyVaultUri => new Uri(KeyVaultUriString);


        public SecretFetcher(string keyVaultUriString, string secretName)
        {
            KeyVaultUriString = keyVaultUriString;
            SecretName = secretName;
        }


        public async Task<string> GetKeyValueAsync()
        {
            // DefaultAzureCredential을 사용하면 Managed Identity를 통해 자동으로 인증이 이루어짐
            var client = new SecretClient(KeyVaultUri, new DefaultAzureCredential());

            // 비밀 값 가져오기
            KeyVaultSecret secret = await client.GetSecretAsync(SecretName);
            return secret.Value;
        }


        public async Task<string> GetKeyValueAsync(TokenCredential tokenCredential)
        {
            var client = new SecretClient(KeyVaultUri, tokenCredential);

            // 비밀 값 가져오기
            KeyVaultSecret secret = await client.GetSecretAsync(SecretName);
            return secret.Value;
        }
    }


    // 같은 KeyVaultUri 일 경우 한번만 접근하기 위해서
    internal class MultiSecretFetcher
    {
        private List<SecretFetcher> _secretFetchers;

        // params를 사용한 생성자
        public MultiSecretFetcher(params SecretFetcher[] secretFetchers)
        {
            _secretFetchers = new List<SecretFetcher>(secretFetchers);
        }


    }
}
