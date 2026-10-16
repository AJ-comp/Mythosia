namespace Mythosia.Diagnostics.Tests
{
    [TestClass()]
    public class StringExtensionTests
    {
        [TestMethod()]
        public async Task ExecuteCommandAsync_ReturnsStandardOutput()
        {
            CommandResult result = await "echo mythosia-checkout".ExecuteCommandAsync();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0, result.ExitCode);
            Assert.AreEqual("mythosia-checkout", result.Result.Trim());
            Assert.AreEqual(string.Empty, result.Error);
        }

        [TestMethod()]
        public async Task ExecuteCommandAsync_CapturesStandardError()
        {
            CommandResult result = await "echo mythosia-error 1>&2".ExecuteCommandAsync();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0, result.ExitCode);
            Assert.AreEqual(string.Empty, result.Result);
            Assert.AreEqual("mythosia-error", result.Error.Trim());
        }

        [TestMethod()]
        public async Task ExecuteCommandAsync_ReturnsFailureExitCode()
        {
            CommandResult result = await "exit 7".ExecuteCommandAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(7, result.ExitCode);
        }
    }
}
