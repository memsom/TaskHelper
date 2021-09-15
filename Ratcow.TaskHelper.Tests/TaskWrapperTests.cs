/*
 * Copyright 2017 Rat Cow Software and Matt Emson. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 *    conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 *    of conditions and the following disclaimer in the documentation and/or other materials
 *    provided with the distribution.
 * 3. Neither the name of the Rat Cow Software nor the names of its contributors may be used
 *    to endorse or promote products derived from this software without specific prior written
 *    permission.
 *
 * THIS SOFTWARE IS PROVIDED BY RAT COW SOFTWARE "AS IS" AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 * ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those of the
 * authors and should not be interpreted as representing official policies, either expressed
 * or implied, of Rat Cow Software and Matt Emson.
 *
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Ratcow.TaskHelper.Tests
{
    [TestClass]
    public class TaskWrapperTests
    {
        [TestMethod]
        public void TaskWrapper_BasicTest()
        {
            var task = new TaskWrapper();

            task.Run();

            Thread.Sleep(2000);

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BasicTask_Test()
        {
            var task = new BasicTask();
            task.Input.AddRange(new string[] { "one", "two", "three", "four", "five" });

            task.Run();

            Thread.Sleep(2000);

            for(var i = 0; i < task.Output.Count; i++)
            {
                Assert.AreEqual(task.Input[i], task.Output[i]);
            }

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BasicSumTask_Test()
        {
            var task = new BasicSumTask();
            task.Input.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            task.Run();

            Thread.Sleep(2000);

            Assert.AreEqual(task.Output, 45);
            
            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BasicTask_TestWithWait()
        {
            var task = new BasicWaitableTask();
            task.Input.AddRange(new string[] { "one", "two", "three", "four", "five" });

            task.Run();

            task.Wait();

            for (var i = 0; i < task.Output.Count; i++)
            {
                Assert.AreEqual(task.Input[i], task.Output[i]);
            }

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BasicSumTask_TestWithWait()
        {
            var task = new BasicWaitableSumTask();
            task.Input.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            task.Run();

            task.Wait();

            Assert.AreEqual(task.Output, 45);

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BasicTask_TestWithWaitTimeout()
        {
            var task = new BasicWaitableTask();
            task.Input.AddRange(new string[] { "one", "two", "three", "four", "five" });

            task.Run();

            var result = task.Wait(3000);
            Assert.IsTrue(result);

            for (var i = 0; i < task.Output.Count; i++)
            {
                Assert.AreEqual(task.Input[i], task.Output[i]);
            }

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BasicSumTask_TestWithWaitTimeout()
        {
            var task = new BasicWaitableSumTask();
            task.Input.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            task.Run();

            var result = task.Wait(3000);
            Assert.IsTrue(result);

            Assert.AreEqual(task.Output, 45);

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BadTask_TestWithWaitTimeout()
        {
            var task = new BadTask();

            task.Run();

            var result = task.Wait(3000);
            Assert.IsFalse(result);

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_BadTask_TestWithWaitTimeoutCancellation()
        {
            var task = new BadTask();

            task.Run();

            var result = task.Wait(3000, true);
            Assert.IsFalse(result);
            Assert.IsFalse(task.Running); //task should have been torn down

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_PeriodicTestTask_TestWithWaitTimeout()
        {
            var task = new PeriodicTestTask
            {
                WaitInMilliseconds = 500
            };

            task.Run();

            var result = task.Wait(3000, true);
            Assert.IsFalse(result); //we terminated the task
            Assert.IsTrue(task.Counter != 0); //we hope it ran
            Assert.IsTrue(task.Counter >= 6); //we waited for 3 seconds, so it should have ticked at least 6 times

            task.Dispose();
        }

        [TestMethod]
        public void TaskWrapper_PeriodicEventTask_TestWithWaitTimeout()
        {
            var counter = 0;

            var task = new PeriodicEventTask
            {
                WaitInMilliseconds = 500                
            };

            task.TaskIteration += () =>
            {
                counter++;
            };

            task.Run();

            var result = task.Wait(3000, true);
            Assert.IsFalse(result); //we terminated the task
            Assert.IsTrue(counter != 0); //we hope it ran
            Assert.IsTrue(counter >= 6); //we waited for 3 seconds, so it should have ticked at least 6 times

            task.Dispose();
        }
    }
}
