using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FsFilter1UI
{
    class Folder
    {
        public Folder(string path, bool encrypt, bool block, bool encrypted)
        {
            this.path = path;
            this.encrypt = encrypt;
            this.block = block;
            this.encrypted = encrypted;
        }
        public string path { get; }
        public bool encrypt { get; set; }
        public bool block { get; set; }

        public bool encrypted { get; set; }
    }
}
