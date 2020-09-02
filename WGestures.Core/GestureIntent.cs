using System;
using System.Diagnostics;
using WGestures.Common.OsSpecific.Windows;
using WGestures.Core.Commands;

namespace WGestures.Core
{
    /// <summary>
    ///     Gesture + Command + Context
    /// </summary>
    [Serializable]
    public class GestureIntent
    {
        public Gesture Gesture { get; set; }
        public AbstractCommand Command { get; set; }

        public bool ExecuteOnModifier { get; set; }


        public string Name { get; set; }

        public bool CanExecuteOnModifier()
        {
            return this.Gesture.Modifier != GestureModifier.None && this.ExecuteOnModifier;
        }

        public ExecutionResult Execute(GestureContext context, GestureParser gestureParser)
        {
            //Aware接口依赖注入
            var contextAware = this.Command as IGestureContextAware;
            if (contextAware != null)
            {
                contextAware.Context = context;
            }

            var parserAware = this.Command as IGestureParserAware;
            if (parserAware != null)
            {
                parserAware.Parser = gestureParser;
            }

            var shouldInit = this.Command as INeedInit;
            if (shouldInit != null && !shouldInit.IsInitialized)
            {
                shouldInit.Init();
            }

            //在独立线程中运行
            //new Thread似乎反应快一点，ThreadPool似乎有延迟
            //ThreadPool.QueueUserWorkItem((s) =>
            //new Thread(() =>
            {
                try
                {
                    context.ActivateTargetWindow();
                    this.Command.Execute();
                    using (var proc = Process.GetCurrentProcess())
                    {
                        Native.SetProcessWorkingSetSize(proc.Handle, -1, -1);
                    }
                }
                catch (Exception)
                {
                    //ignore errors for now
#if DEBUG
                    throw;
#endif
                }
            } //) { IsBackground = false}.Start();

            return new ExecutionResult(null, true);
        }

        public class ExecutionResult
        {
            public ExecutionResult(Exception e, bool isOk)
            {
                this.Exception = e;
                this.IsOk = isOk;
            }

            public Exception Exception { get; }
            public bool IsOk { get; }
        }
    }
}
