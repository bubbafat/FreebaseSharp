﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseSharp.Portable.Interfaces
{
    public interface IDataSnapshot
    {
        bool Exists { get; }
        IDataSnapshot Child(string childName);
        IEnumerable<IDataSnapshot> Children { get; }
        bool HasChildren { get; }
        int NumChildren { get; }
        IFirebase Ref();
        FirebasePriority GetPriority();
        string Key { get; }
        string ExportVal();
        T Value<T>() where T : struct;
        string Value();

        IDataSnapshot this[string child] { get; }

    }
}
