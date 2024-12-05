using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mythosia.Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mythosia.Azure.Storage.Blobs.Tests
{
    [TestClass()]
    public class StringExtensionTests
    {
        [TestMethod()]
        public void ToBlobContainerNameTest()
        {
            var test1 = "MythosiaAzureStorageBlobs".ToBlobContainerName();
            var test2 = "mythosia-azure-storage-blobs".ToBlobContainerName();
            var test3 = "MythosiaAzureStorageBlobs123".ToBlobContainerName();
            var test4 = "MythosiaAzureStorageBlobs123456789012345678901234567890123456789012345678901234567890".ToBlobContainerName();
            var test5 = "MythosiaAzureStorageBlobs@@@1234567890123456789012345678901234567890123456789012345678901".ToBlobContainerName();
        }
    }
}