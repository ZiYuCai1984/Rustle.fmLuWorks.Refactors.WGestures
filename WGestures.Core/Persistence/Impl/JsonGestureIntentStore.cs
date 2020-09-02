using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WGestures.Core.Commands;

//using Newtonsoft.Json;

namespace WGestures.Core.Persistence.Impl
{
    public class JsonGestureIntentStore : IGestureIntentStore
    {
        private readonly JsonSerializer ser = new JsonSerializer();
        private string jsonPath;

        private JsonGestureIntentStore()
        {
        }

        public JsonGestureIntentStore(string jsonPath, string fileVersion)
        {
            this.FileVersion = fileVersion;
            this.jsonPath = jsonPath;
            this.SetupSerializer();

            if (File.Exists(jsonPath))
            {
                this.Deserialize();
            }
            else
            {
                this.Apps = new Dictionary<string, ExeApp>();
                this.GlobalApp = new GlobalApp();
                this.HotCornerCommands = new AbstractCommand[8]; //4 corners + 4 edges
            }
        }

        public JsonGestureIntentStore(Stream stream, bool closeStream, string fileVersion)
        {
            this.FileVersion = fileVersion;
            this.SetupSerializer();
            this.Deserialize(stream, closeStream);
        }

        public string FileVersion { get; set; }
        public Dictionary<string, ExeApp> Apps { get; set; }
        public GlobalApp GlobalApp { get; set; }
        public AbstractCommand[] HotCornerCommands { get; set; } //4 corners + 4 edges


        public bool TryGetExeApp(string key, out ExeApp found)
        {
            return this.Apps.TryGetValue(key.ToLower(), out found);
        }

        public ExeApp GetExeApp(string key)
        {
            return this.Apps[key.ToLower()];
        }


        public void Remove(string key)
        {
            this.Apps.Remove(key.ToLower());
        }

        public void Remove(ExeApp app)
        {
            this.Remove(app.ExecutablePath.ToLower());
        }

        public void Add(ExeApp app)
        {
            app.ExecutablePath = app.ExecutablePath.ToLower();
            this.Apps.Add(app.ExecutablePath, app);
        }

        public void Save()
        {
            this.Serialize();
        }

        public IEnumerator<ExeApp> GetEnumerator()
        {
            return this.Apps.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void Deserialize(Stream stream, bool closeStream)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("stream");
            }

            try
            {
                using (var txtReader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(txtReader))
                {
                    /*var ser = new JsonSerializer();
                    ser.Formatting = Formatting.None;
                    ser.TypeNameHandling = TypeNameHandling.Auto;

                    if (FileVersion.Equals("1"))
                    {
                        ser.Converters.Add(new GestureIntentConverter_V1());

                    }
                    else// if (FileVersion.Equals("2"))
                    {
                        ser.Converters.Add(new GestureIntentConverter());

                    }*/
                    var result = ser.Deserialize<SerializeWrapper>(jsonReader);

                    this.FileVersion = result.FileVersion;
                    this.GlobalApp = result.Global;
                    //Apps = result.Apps;

                    this.Apps = new Dictionary<string, ExeApp>();

                    //to lower
                    foreach (var a in result.Apps.Values)
                    {
                        a.ExecutablePath = a.ExecutablePath.ToLower();
                        this.Apps.Add(a.ExecutablePath, a);
                    }

                    //convert old version GestureButton Value ( 0->1, 1->2)
                    if (this.FileVersion == "1" || this.FileVersion == "2")
                    {
                        var globalIntents = this.GlobalApp.GestureIntents.Values.ToArray();
                        this.GlobalApp.GestureIntents.Clear();

                        foreach (var gestIntent in globalIntents)
                        {
                            gestIntent.Gesture.GestureButton += 1;
                            this.GlobalApp.GestureIntents.Add(gestIntent);
                        }

                        foreach (var app in this.Apps.Values)
                        {
                            var intents = app.GestureIntents.Values.ToArray();
                            app.GestureIntents.Clear();

                            foreach (var gestIntent in intents)
                            {
                                gestIntent.Gesture.GestureButton += 1;
                                app.GestureIntents.Add(gestIntent);
                            }
                        }
                    }

                    this.HotCornerCommands = new AbstractCommand[8];
                    Array.Copy(
                        result.HotCornerCommands,
                        this.HotCornerCommands,
                        result.HotCornerCommands.Length);
                    //HotCornerCommands = result.HotCornerCommands;
                }
            }
            finally
            {
                if (closeStream)
                {
                    stream.Dispose();
                }
            }

            //todo: 完全在独立domain中加载json.net?
            /*var deserializeDomain = AppDomain.CreateDomain("jsonDeserialize");
            deserializeDomain.UnhandledException += (sender, args) => { throw new IOException(args.ExceptionObject.ToString()); };
            deserializeDomain.DomainUnload += (sender, args) =>
            {
                Console.WriteLine("deserializeDomain Unloaded");
            };
            var wrapperRef = (ISerializeWrapper)deserializeDomain.CreateInstanceAndUnwrap("SerializeWrapper", "SerializeWrapper.SerializeWrapper");

            wrapperRef.DeserializeFromStream(stream, FileVersion, closeStream);

            GlobalApp = wrapperRef.Global;
            Apps = wrapperRef.Apps;

            wrapperRef = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            AppDomain.Unload(deserializeDomain);
            deserializeDomain = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();*/
        }

        private void Serialize()
        {
            using (var fs = new StreamWriter(jsonPath))
            {
                using (var writer = new JsonTextWriter(fs))
                {
                    ser.Serialize(
                        writer,
                        new SerializeWrapper
                        {
                            Apps = this.Apps, FileVersion = this.FileVersion,
                            Global = this.GlobalApp, HotCornerCommands = this.HotCornerCommands
                        });
                }
            }

            //todo: 完全在独立domain中加载json.net?
            /*var serializeDomain = AppDomain.CreateDomain("jsonDeserialize");
            serializeDomain.UnhandledException += (sender, args) => { throw new IOException(args.ExceptionObject.ToString()); };

            serializeDomain.DomainUnload += (sender, args) =>
            {
                Console.WriteLine("serializeDomain Unloaded");
            };
            var wrapperRef = (ISerializeWrapper)serializeDomain.CreateInstanceAndUnwrap("SerializeWrapper", "SerializeWrapper.SerializeWrapper");

            wrapperRef.FileVersion = FileVersion;
            wrapperRef.Apps = Apps;
            wrapperRef.Global = GlobalApp;
            wrapperRef.SerializeTo(jsonPath);

            wrapperRef = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            AppDomain.Unload(serializeDomain);
            serializeDomain = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();*/
        }

        private void Deserialize()
        {
            using (var file = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            {
                this.Deserialize(file, false);
            }
        }

        private void SetupSerializer()
        {
            ser.Formatting = Formatting.None;
            ser.TypeNameHandling = TypeNameHandling.Auto;

            if (this.FileVersion.Equals("1"))
            {
                ser.Converters.Add(new GestureIntentConverter_V1());
            }
            else //if (FileVersion.Equals("2"))
            {
                ser.Converters.Add(new GestureIntentConverter());
            }
        }

        public JsonGestureIntentStore Clone()
        {
            //fixme: dummy impl
            var ret = new JsonGestureIntentStore();
            ret.GlobalApp = this.GlobalApp;
            ret.Apps = this.Apps;
            ret.FileVersion = this.FileVersion;
            ret.jsonPath = jsonPath;
            ret.HotCornerCommands = this.HotCornerCommands;
            return ret;
        }

        public void Import(JsonGestureIntentStore from, bool replace = false)
        {
            if (from == null)
            {
                return;
            }

            if (replace)
            {
                this.GlobalApp.GestureIntents.Clear();
                this.GlobalApp.IsGesturingEnabled = from.GlobalApp.IsGesturingEnabled;
                this.Apps.Clear();
                this.HotCornerCommands = from.HotCornerCommands;
            }
            else
            {
                for (var i = 0; i < from.HotCornerCommands.Length; i++)
                {
                    if (from.HotCornerCommands[i] != null)
                    {
                        this.HotCornerCommands[i] = from.HotCornerCommands[i];
                    }
                }
            }

            this.GlobalApp.ImportGestures(from.GlobalApp);

            foreach (var kv in from.Apps)
            {
                ExeApp appInSelf;
                //如果应用程序已经在列表中，则合并手势
                if (this.TryGetExeApp(kv.Key.ToLower(), out appInSelf))
                {
                    appInSelf.ImportGestures(kv.Value);
                    appInSelf.IsGesturingEnabled =
                        appInSelf.IsGesturingEnabled && kv.Value.IsGesturingEnabled;
                }
                else //否则将app添加到列表中
                {
                    this.Add(kv.Value);
                }
            }
        }

        internal class SerializeWrapper
        {
            public string FileVersion { get; set; }
            public Dictionary<string, ExeApp> Apps { get; set; }
            public GlobalApp Global { get; set; }
            public AbstractCommand[] HotCornerCommands { get; set; } = new AbstractCommand[8];
        }

        internal class GestureIntentConverter : JsonConverter
        {
            public override void WriteJson(
                JsonWriter writer, object value, JsonSerializer serializer)
            {
                var dict = value as GestureIntentDict;
                serializer.Serialize(writer, dict.Values.ToList());
            }

            public override object ReadJson(
                JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var dict = new GestureIntentDict();
                var list = serializer.Deserialize<List<GestureIntent>>(reader);
                foreach (var i in list)
                {
                    dict.Add(i);
                }

                return dict;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(GestureIntentDict);
            }
        }

        //.json
        internal class GestureIntentConverter_V1 : JsonConverter
        {
            public override void WriteJson(
                JsonWriter writer, object value, JsonSerializer serializer)
            {
                var dict = value as GestureIntentDict;
                serializer.Serialize(writer, dict.Values.ToList());
            }

            public override object ReadJson(
                JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var dict = new GestureIntentDict();
                var list =
                    serializer.Deserialize<List<KeyValuePair<Gesture, GestureIntent>>>(reader);
                foreach (var i in list)
                {
                    Debug.WriteLine("Add Gesture: " + i.Value.Gesture);
                    dict.Add(i.Value);
                }

                return dict;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(GestureIntentDict);
            }
        }

        /*
        public interface ISerializeWrapper
        {
             string FileVersion { get; set; }
             Dictionary<string, ExeApp> Apps { get; set; }
             GlobalApp Global { get; set; }

             void DeserilizeFromFile(string filename, string version);
            void DeserializeFromStream(Stream s, string version, bool close = false);
             void SerializeTo(string fileName);
        }*/
    }
}
