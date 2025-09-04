namespace DirectorySynchronizer.Tests
{
    public class E2EInvalidArgsTest : EndToEndTest
    {
        [Fact]
        public void E2E_InvalidArgs_Test()
        {
            string[] args = ["arg1", "arg2"];

            var errWriter = new StringWriter();

            RunProgram(args, 1, testWriter, errWriter).Join();

            var output = testWriter.ToString();
            Assert.Contains("Usage:", output);

            var errOutput = errWriter.ToString();
            Assert.Contains("Invalid number of arguments", errOutput);

            errWriter.Dispose();
        }
    }
}