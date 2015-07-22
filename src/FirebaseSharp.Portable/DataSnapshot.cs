﻿using System;
using System.Collections.Generic;
using System.Linq;
using FirebaseSharp.Portable.Interfaces;
using Newtonsoft.Json.Linq;

namespace FirebaseSharp.Portable
{
    class DataSnapshot : IDataSnapshot
    {
        private readonly JToken _token;
        private readonly FirebaseApp _app;
        private readonly FirebasePath _path;
        internal DataSnapshot(FirebaseApp app, FirebasePath path, JToken token)
        {
            _token = token != null ? token.DeepClone() : null;
            _app = app;
            _path = path;
        }

        internal JToken Token
        {
            get { return _token; }
        }

        public bool Exists
        {
            get { return _token != null; }
        }
        public IDataSnapshot Child(string childName)
        {
            JToken child = null;

            if (_token != null)
            {
                foreach (string childPath in new FirebasePath(childName).Segments)
                {
                    child = _token.First[childPath];
                    if (child == null)
                    {
                        break;
                    }
                }
            }

            return new DataSnapshot(_app, _path.Child(childName), child);
        }

        public IEnumerable<IDataSnapshot> Children
        {
            get
            {
                if (_token == null)
                {
                    return new List<IDataSnapshot>();
                }

                return _token.Children().Select(t => new DataSnapshot(_app, _path.Child(t.Path), t));
            }
        }

        public bool HasChildren
        {
            get
            {
                return (_token != null) && _token.Children().Any();
            }
        }

        public int NumChildren
        {
            get
            {
                return (_token == null) ? 0 : _token.Children().Count();
            }
        }

        public IFirebase Ref()
        {
            return new Firebase(_app, _path);
        }

        public FirebasePriority GetPriority()
        {
            return new FirebasePriority(_token );
        }
        public string Key { get { return _path.Key; } }

        public T Value<T>() where T: struct
        {
            string value = GetValueString();

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public string Value()
        {
            return GetValueString();
        }

        public IDataSnapshot this[string child]
        {
            get { return Child(child); }
        }

        public string ExportVal()
        {
            // TODO: trim priority
            return Value();
        }

        private string GetValueString()
        {
            JProperty jp = _token as JProperty;
            if (jp != null)
            {
                return jp.Value.ToString();
            }

            JValue jv = _token as JValue;
            if (jv != null)
            {
                return jv.Value.ToString();
            }

            return _token.ToString();
        }
    }
}
