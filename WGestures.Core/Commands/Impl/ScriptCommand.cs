using System;
using System.Diagnostics;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLua;
using NLua.Exceptions;
using WGestures.Common.Annotation;

namespace WGestures.Core.Commands.Impl
{
    [Named("Lua脚本")]
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ScriptCommand
        : AbstractCommand, IGestureModifiersAware, INeedInit, IGestureContextAware
    {
        private string _initScript;
        private Lua _state;


        [JsonProperty]
        public string InitScript
        {
            get => _initScript;
            set
            {
                _initScript = value;
                this.IsInitialized = false; //需要重新初始化
            }
        }

        [JsonProperty] public string Script { get; set; }

        [JsonProperty] public bool HandleModifiers { get; set; }

        //for modified gestures
        [JsonProperty] public string GestureRecognizedScript { get; set; }

        [JsonProperty] public string ModifierTriggeredScript { get; set; }

        [JsonProperty] public string GestureEndedScript { get; set; }

        public GestureContext Context { set; get; }

        public event Action<string> ReportStatus;

        public void GestureRecognized(out GestureModifier observeModifiers)
        {
            if (this.HandleModifiers && this.GestureRecognizedScript != null)
            {
                var retVals = this.DoString(this.GestureRecognizedScript, "GestureRecognized");
                if (retVals.Length > 0)
                {
                    var number = (int) (GestureModifier) retVals[0];

                    var gm = (GestureModifier) number;
                    Debug.WriteLine("observeModifier=" + gm);
                    observeModifiers = gm;
                    return;
                }
            }

            observeModifiers = GestureModifier.None;
        }

        public void ModifierTriggered(GestureModifier modifier)
        {
            if (this.HandleModifiers && this.ModifierTriggeredScript != null)
            {
                _state["modifier"] = modifier;
                this.DoString(this.ModifierTriggeredScript, "ModifierTriggered");
                _state["modifier"] = null;
            }
        }

        public void GestureEnded()
        {
            if (this.HandleModifiers && this.GestureEndedScript != null)
            {
                this.DoString(this.GestureEndedScript, "GestureEnded");
            }
        }

        //INeedInit
        public bool IsInitialized { get; private set; }

        public void Init()
        {
            if (this.IsInitialized)
            {
                throw new InvalidOperationException("Already Initialized!");
            }

            if (_state != null)
            {
                _state.Dispose();
            }

            _state = new Lua();
            _state.LoadCLRPackage();

            _state.DoString(
                @"luanet.load_assembly('WGestures.Core');
                              luanet.load_assembly('WindowsInput');
                              luanet.load_assembly('WGestures.Common');

                              GestureModifier=luanet.import_type('WGestures.Core.GestureModifier');  
                              VK=luanet.import_type('WindowsInput.Native.VirtualKeyCode');
                              Native=luanet.import_type('WGestures.Common.OsSpecific.Windows.Native');
                            ",
                "_init");

            _state["Input"] = Sim.Simulator;
            _state.RegisterFunction(
                "ReportStatus",
                this,
                typeof(ScriptCommand).GetMethod("OnReportStatus"));

            if (this.InitScript != null)
            {
                this.DoString(this.InitScript, "Init");
            }

            this.IsInitialized = true;
        }

        public override void Execute()
        {
            if (!this.IsInitialized)
            {
                throw new InvalidOperationException("I Need Init!");
            }

            if (this.Script != null)
            {
                this.DoString(this.Script, "Execute");
            }
        }

        private object[] DoString(string script, string name)
        {
            _state["Context"] = this.Context;
            try
            {
                return _state.DoString(script, name);
            }
            catch (LuaScriptException e)
            {
                Console.WriteLine(e.InnerException);
                MessageBox.Show(e.ToString(), "Lua脚本错误");
                return new object[0];
            }
        }

        public void OnReportStatus(string s)
        {
            if (this.ReportStatus != null)
            {
                this.ReportStatus(s);
            }
        }
    }

    //helper methods
    namespace _ExportToLua
    {
        public static class WG
        {
            public static void HelloWorld()
            {
                MessageBox.Show("Hello World!");
            }
        }
    }
}
