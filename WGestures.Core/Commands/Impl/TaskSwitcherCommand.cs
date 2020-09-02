using System;
using System.Diagnostics;
using System.Threading;
using WindowsInput.Native;
using WGestures.Common.Annotation;
using WGestures.Common.OsSpecific.Windows;

namespace WGestures.Core.Commands.Impl
{
    [Named("任务切换")]
    [Serializable]
    public class TaskSwitcherCommand : AbstractCommand, IGestureModifiersAware
    {
        public void GestureRecognized(out GestureModifier observeModifiers)
        {
            //直接交由系统的任务切换机制处理，不需要订阅任何事件
            observeModifiers = GestureModifier.None;

            try
            {
                Sim.KeyDown(VirtualKeyCode.LMENU);
                Sim.KeyPress(VirtualKeyCode.TAB);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("发送按键失败: " + ex);
                this.TryRecoverAltTab();
            }
        }

        public void ModifierTriggered(GestureModifier modifier)
        {
        }

        public void GestureEnded()
        {
            try
            {
                Sim.KeyUp(VirtualKeyCode.LMENU);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("发送按键失败： " + ex);
                this.TryRecoverAltTab();
            }
        }

        public event Action<string> ReportStatus;

        public override void Execute()
        {
            try
            {
                Sim.KeyDown(VirtualKeyCode.LMENU);

                //Thread.Sleep(50);
                Sim.KeyDown(VirtualKeyCode.TAB);

                //如果不延迟，foxitreader等ctrl+C可能失效！
                Thread.Sleep(20);

                Sim.KeyUp(VirtualKeyCode.TAB);

                Sim.KeyUp(VirtualKeyCode.LMENU);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("发送alt + tab失败: " + ex);
                this.TryRecoverAltTab();
#if TEST
                throw;
#endif
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        private void TryRecoverAltTab()
        {
            Debug.WriteLine("尝试恢复Alt Tab的状态...");
            Native.TryResetKeys(new[] {VirtualKeyCode.LMENU, VirtualKeyCode.TAB});
        }
    }
}
