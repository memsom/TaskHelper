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
using System.Threading;
using System.Threading.Tasks;

namespace Ratcow.TaskHelper
{
    /// <summary>
    /// Simple disposable task wrapper with cancellation
    /// </summary>
    public class TaskWrapper : IDisposable, ITaskWrapper
    {
        protected CancellationTokenSource cancellationSource;
        protected CancellationToken cancellationToken;
        protected Task task;        
        protected object syncObject = new object();
        protected object stopped = false;

        protected bool isCancelled = false;

        /// <summary>
        /// Is the Task running?
        /// </summary>
        public virtual bool Running
        {
            get
            {
                return task != null;
            }
        }

        /// <summary>
        /// Just to make this a little prettier
        /// </summary>
        protected bool IsCancelled
        {
            get
            {
                if (task != null)
                {
                    isCancelled = (task?.IsCanceled ?? true) || cancellationToken.IsCancellationRequested;
                }

                return isCancelled;
            }
        }

        protected bool IsStopped
        {
            get
            {
                lock (stopped)
                {
                    if (task == null)
                    {
                        return true;
                    }

                    return (bool)stopped || (task?.IsCanceled ?? true);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            lock (syncObject)
            {
                if (task == null)
                {
                    cancellationSource = new CancellationTokenSource();
                    cancellationToken = cancellationSource.Token;

                    task = Task.Factory.StartNew(() =>
                    {
                        ExecuteTaskLoop();
                    },
                        cancellationToken
                    )
                    .ContinueWith((t) =>
                    {
                        try
                        {
                            t.Wait();
                        }
                        catch (AggregateException ae)
                        {
                            if (!(ae.InnerException is OperationCanceledException oce))
                            {
                                lock (stopped)
                                {
                                    if (!t.IsCanceled && !(bool)stopped)
                                    {
                                        cancellationSource.Dispose();
                                        task = null;
                                    }
                                }
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// This is private - dispose the class to stop the task
        /// </summary>
        protected void Stop()
        {
            lock (syncObject)
            {
                lock (stopped)
                {
                    stopped = true;
                }

                if (task != null)
                {
                    cancellationSource.Cancel();
                    cancellationSource.Dispose();
                    task.Wait();
                    task.Dispose();
                    task = null;
                }

                lock (stopped)
                {
                    stopped = false;
                }
            }
        }        

        /// <summary>
        /// Override this and add in the task code to execute
        /// </summary>
        protected virtual void ExecuteTaskLoop()
        {

        }

        /// <summary>
        /// Override this to dispose managed code 
        /// 
        /// Called *after* the Stop method is called, 
        /// the Task will already be cleaned up.
        /// </summary>
        protected virtual void DisposeManaged()
        {

        }

        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();

                    DisposeManaged();
                }

                disposedValue = true;
            }
        }

        ~TaskWrapper()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public virtual void Dispose()
        {
            // Do not change this code. Put clean-up code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
