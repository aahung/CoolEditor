﻿using System.Collections.Generic;

namespace CoolEditor.Class.DropNetRt.Models
{
    internal class DeltaPageInternal
    {
        public string Cursor { get; set; }
        public bool Has_More { get; set; }
        public bool Reset { get; set; }
        public List<List<string>> Entries { get; set; }
    }
}
