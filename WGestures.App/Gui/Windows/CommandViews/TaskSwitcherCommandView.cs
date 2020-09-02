using System;
using System.Drawing;
using WGestures.Core.Commands;
using WGestures.Core.Commands.Impl;

namespace WGestures.App.Gui.Windows.CommandViews
{
    internal class TaskSwitcherCommandView : CommandViewUserControl
    {
        private TaskSwitcherCommand _command;

        public TaskSwitcherCommandView()
        {
            this.InitializeComponent();
        }

        public override AbstractCommand Command
        {
            get => _command;
            set => _command = (TaskSwitcherCommand) value;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TaskSwitcherCommandView
            // 
            this.AutoScaleDimensions = new SizeF(6F, 12F);
            this.Name = "TaskSwitcherCommandView";
            this.ResumeLayout(false);
        }

        private void check_prevTask_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}
