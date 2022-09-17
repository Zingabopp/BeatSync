using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BeatSyncLibTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            bool continueWorked = false;
            var task = Task.Run(async () =>
            {
                await Task.Delay(500);
                throw new Exception();
            });
            var continuation = task.ContinueWith(t =>
            {
                continueWorked = true;
            });
            await Assert.ThrowsExceptionAsync<Exception>(() => task);
            Assert.IsTrue(continueWorked);
            continueWorked = false;
            await task.ContinueWith(t =>
            {
                continueWorked = true;
            });
            Assert.IsTrue(continueWorked);
            await continuation;
        }
    }
}
