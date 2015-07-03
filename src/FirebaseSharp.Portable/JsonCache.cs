﻿using System;
using Newtonsoft.Json.Linq;

namespace FirebaseSharp.Portable
{
    internal delegate void DataChangedHandler(object sender, DataChangedEventArgs e);

    enum ChangeSource
    {
        Local,
        Remote
    }

    internal class DataChangedEventArgs : EventArgs
    {
        public DataChangedEventArgs(ChangeSource source, EventType eventType, string path, string data, string oldData = null)
        {
            Source = source;
            Path = path;
            Data = data;
            Event = eventType;
            OldData = oldData;
        }

        public ChangeSource Source { get; private set; }

        public EventType Event { get; private set; }
        public string Path { get; private set; }
        public string Data { get; private set; }
        public string OldData { get; private set; }
    }

    enum EventType
    {
        Added,
        Changed,
        Removed
    }

    class JsonCache
    {
        private JToken _root = null;

        public event DataChangedHandler Changed;

        public void Put(ChangeSource source, string path, string data)
        {
            if (data == null)
            {
                Delete(source, path);
                return;
            }

            JToken newData = data.Trim().StartsWith("{")
                ? JToken.Parse(data)
                : new JValue(data);

            JToken found;
            if (TryGetChild(path, out found))
            {
                JToken old = found.DeepClone();
                if (!UpdateValues(found, newData))
                {
                    if (found.Parent != null)
                    {
                        found.Replace(newData);
                    }
                    else
                    {
                        _root = newData;
                    }
                }

                OnChanged(new DataChangedEventArgs(source, EventType.Changed, path, newData.ToString(), old.ToString()));
            }
            else
            {
                var inserted = InsertAt(path, newData);
                OnChanged(new DataChangedEventArgs(source, EventType.Added, path, inserted.ToString()));
            }
        }

        public void Patch(ChangeSource source, string path, string data)
        {
            if (data == null)
            {
                Delete(source, path);
                return;
            }

            JToken newData = data.Trim().StartsWith("{")
                ? JToken.Parse(data)
                : new JValue(data);

            JToken found;
            if (TryGetChild(path, out found))
            {
                JToken old = found.DeepClone();
                if (data.Trim().StartsWith("{"))
                {
                    Merge(found, newData);
                }
                else
                {
                    if (!UpdateValues(found, newData))
                    {
                        if (found.Parent != null)
                        {
                            found.Replace(newData);
                        }
                        else
                        {
                            _root = newData;
                        }
                    }
                }

                OnChanged(new DataChangedEventArgs(source, EventType.Changed, path, found.ToString(), old.ToString()));
            }
            else
            {
                var inserted = InsertAt(path, newData);
                OnChanged(new DataChangedEventArgs(source, EventType.Added, path, inserted.ToString()));
            }
        }

        private bool UpdateValues(JToken oldToken, JToken newToken)
        {
            JValue oldVal = oldToken as JValue;
            JValue newVal = newToken as JValue;

            if (oldVal != null && newVal != null)
            {
                oldVal.Value = newVal.Value;
                return true;
            }

            return false;
        }
        private JToken InsertAt(string path, JToken newData)
        {
            string[] segments = NormalizePath(path).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length > 0)
            {
                if (_root == null)
                {
                    _root = new JObject();
                }

                var node = _root;

                for (int i = 0; i < segments.Length - 1; i++)
                {
                    string segment = segments[i];
                    var child = node[segment];
                    if (child == null)
                    {
                        node[segment] = new JObject();
                    }
                }

                node[segments[segments.Length - 1]] = newData;
                return node[segments[segments.Length - 1]];
            }
            else
            {
                _root = newData;
                return _root;
            }
        }

        private void Merge(JToken target, JToken newData)
        {
            foreach (var newChildPath in newData.Children())
            {
                var existingTarget = target[newChildPath.Path];
                var newChild = newData[newChildPath.Path];

                if (existingTarget != null)
                {
                    JValue existingValue = existingTarget as JValue;

                    if (existingValue != null)
                    {
                        JValue newValue = newChild as JValue;
                        if (newValue != null)
                        {
                            existingValue.Replace(newValue);
                            continue;
                        }
                    }
                }

                target[newChild.Path] = newChild;
            }
        }

        public void Delete(ChangeSource source, string path)
        {
            JToken node;
            if (TryGetChild(path, out node))
            {
                if (node.Parent != null)
                {
                    node.Parent.Remove();
                }
                else
                {
                    _root = new JObject();
                }

                OnChanged(new DataChangedEventArgs(source, EventType.Removed, path, null));
            }
        }

        private bool TryGetChild(string path, out JToken node)
        {
            string[] segments = NormalizePath(path).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            node = _root;

            if (node != null)
            {
                foreach (var segment in segments)
                {
                    node = node[segment];
                    if (node == null)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private static string NormalizePath(string path)
        {
            return path.TrimStart(new char[] { '/' }).Trim().Replace('/', '.');
        }

        private void OnChanged(DataChangedEventArgs args)
        {
            var handler = Changed;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }
}
