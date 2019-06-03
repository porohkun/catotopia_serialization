using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGenEngine
{
    internal struct ProjectFile
    {
        internal string File { get; private set; }
        internal ItemGroupLabel ItemGroup { get; private set; }
        internal BuildActionType BuildAction { get; private set; }
        internal bool CopyToOutputDirectory { get; private set; }

        public ProjectFile(string file, ItemGroupLabel itemGroup,BuildActionType buildAction,bool copyToOutputDirectory = false)
        {
            File = file.Replace('/','\\');
            ItemGroup = itemGroup;
            BuildAction = buildAction;
            CopyToOutputDirectory = copyToOutputDirectory;
        }

        //internal bool operator ==(ProjectFile a,ProjectFile b)
        //{
        //    return a.File == b.File &&
        //        a.ItemGroup == b.ItemGroup &&
        //        a.BuildAction == b.BuildAction &&
        //        a.CopyToOutputDirectory == b.CopyToOutputDirectory;
        //}
    }
}
