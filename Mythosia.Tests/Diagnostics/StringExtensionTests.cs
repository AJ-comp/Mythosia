namespace Mythosia.Diagnostics.Tests
{
    [TestClass()]
    public class StringExtensionTests
    {
        [TestMethod()]
        public async Task ExecuteCommandAsyncTest()
        {
            string command = "ping google.com"; // Example command
            CommandResult result = await command.ExecuteCommandAsync();

            Console.WriteLine(result);

            Assert.Fail();
        }
    }
}