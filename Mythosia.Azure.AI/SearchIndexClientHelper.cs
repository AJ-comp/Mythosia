using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Threading.Tasks;
using FieldBuilder = Azure.Search.Documents.Indexes.FieldBuilder;

namespace Mythosia
{
    internal static class SearchIndexClientHelper
    {
        public static async Task CreateIndexFromModelAsync<T>(this SearchIndexClient indexClient, string indexName)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(T));

            var index = new SearchIndex(indexName, searchFields);

            await indexClient.CreateOrUpdateIndexAsync(index);
        }
    }
}
