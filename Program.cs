using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Globalization;
using System.Data;

namespace ResCmp
{
    class Program
    {
        static string DEFLANG = "$DEF$";

        static void PrintUsage()
        {
            Console.WriteLine("ResCmp.exe  @copyright: jogels  (build: 20130216)");
            Console.WriteLine("");
            Console.WriteLine("cross-release compare:");
            Console.WriteLine("rescmp.exe /d rtm=<pathToRTMTokenDumps> [v1=<pathToNewerVersion>] [v2=<pathToLatestBuild>] [additional switches]");
            Console.WriteLine("");
            Console.WriteLine("cross-languages compare:");
            Console.WriteLine("rescmp.exe /c en=<pathToENTokenDumps> [de=<pathToDETokenDumps>] [fr=<pathToFRTokenDumps>] [es=<pathToESTokenDumps>] [ja=<pathToJATokenDumps>]  [langCode=<pathToTokenDumps>] [additional switches]");
            Console.WriteLine("");
            Console.WriteLine("automatic cross-languages compare:");
            Console.WriteLine("rescmp.exe /z en=<rootpath> [additional switches]");
            Console.WriteLine("");
            Console.WriteLine("automatic cross-release compare:");
            Console.WriteLine("rescmp.exe /j rtm=<rtmpath1;rtmpath2;...> hrp1=<hrp1path1;hrp1path2;...> [hrp2=<hrp1path1;hrp1path2;...>]... [additional switches]");
            Console.WriteLine("");
            Console.WriteLine("additional switches:");
            Console.WriteLine(" /r\t\t\t -recursive scan");
            Console.WriteLine(" /s\t\t\t -satellite resources structure");
            Console.WriteLine(" /x <comment>\t\t -user comments");
            Console.WriteLine(" /o <pathToOutputDir>\t -output directory");
            Console.WriteLine(" /n <xmlFileName>\t -output filename");
            Console.WriteLine(" /sdv\t\t\t -suppress duplicate values result");
            Console.WriteLine(" /sev\t\t\t -suppress empty values result");
            Console.WriteLine(" /sgi\t\t\t -suppress group items result");
            Console.WriteLine(" /a <lang=code>\t\t -alternative language code e.g. es=1034");
            Console.WriteLine(" /g <suffixString>\t -key suffix string e.g. \"items\"");
            Console.WriteLine(" /m <file1=file2>\t -filename mapping e.g. \"CVhdMount.exe=CVhdMountUI.dll\"");
            Console.WriteLine(" /t <result xml stylesheet>\t -path to XSL file e.g. ResCmp_html.xsl\"");
            Console.WriteLine(" /tm <merged xml stylesheet>\t -path to XSL file e.g. MergedXML.xsl\"");
            Console.WriteLine("");
            Console.WriteLine("[examples]");
            Console.WriteLine("cross-languages comparison:");
            Console.WriteLine("  rescmp.exe /c en=D:\\ZZTMP\\parra_dumps\\xa.mgmt /s /x \"Test Comment\" /o \"D:\\ZZTMP\\Build6334\" /n \"parra.xamgmt.B6334TokenDiff.xml\"");
            Console.WriteLine("");
            Console.WriteLine("cross-release comparison:");
            Console.WriteLine("  rescmp.exe /d rtm=D:\\ZZTMP\\ohio_rtm\\xa.sys32 hrp1=D:\\ZZTMP\\ohio_hrp1\\xa.sys32 /x \"Ohio System32 Files: RTM vs HRP1\" /o \"D:\\ZZTMP\\OhioHRP1\" /n \"rtm_vs_hrp1b17.xml\"");

            ///m en=D:\ZZTMP\parra_dumps\xa.mgmt /s /x "Test Comment" /o "D:\ZZTMP\JJJ" /n "testOut.xml"
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
                PrintUsage();
            else
                RunHander(args);
        }

        //------------ 
        static void AutoProc(Opt op, Mode mod)
        {
            string rootDirPath = op.ReferencePaths[0];
            if (Directory.Exists(rootDirPath))
            {
                Dictionary<string, string> resLocs = new Dictionary<string, string>();
                DirectoryInfo rP = new DirectoryInfo(rootDirPath);
                Console.WriteLine("ResCmp::AutoMode({0}) - {1} ", mod.ToString(), rootDirPath);
                ScanResourceDirectories(rootDirPath, rP, resLocs, true);
                Console.WriteLine("ResCmp::Found " + resLocs.Count + " locations of resource folders.");
                string outDir = op.OutDir;
                Dictionary<string, string[]> rootParams = new Dictionary<string, string[]>();
                op.CopyParams(rootParams);
                foreach (string s in resLocs.Keys)
                {
                    string r = resLocs[s];
                    //string cL = r.Replace('\\', ':');

                    op.Mode = mod; 
                    op.ReferencePaths = new string[] { s };

                    op.IsSRStructure = (Mode.COMP == mod);  //for xLang compare, this should be true
                    op.Comment = r; // cL;
                    string oD = outDir + "\\" + r;

                    if (Mode.DIFF == mod)
                    {
                        string lbl = op.ReferenceKey;
                        foreach (string k in rootParams.Keys) //rtm, hrp1
                        {
                            lbl = lbl + "-" + k;
                            string[] pR = rootParams[k];
                            string[] nR = new string[pR.Length];
                            for (int i = 0; i < pR.Length; i++)  //root1;root2--> newPath1;newPath2
                                nR[i] = pR[i] + "\\" + r;
                            op.ChangeParam(k, nR);
                        }
                        oD = outDir + "\\" + lbl + "\\" + r;
                    }
                    op.OutDir = oD;
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine("Mode:: {0} ", mod.ToString());
                    Console.WriteLine("Ref({0}):: {1} ", op.ReferenceKey, s);
                    foreach (string rt in op.Keys)
                    {
                        Console.WriteLine("Sub({0}):: {1}", rt, String.Join(";", op.Param(rt)));
                    }
                    Console.WriteLine("OutDir:: " + op.OutDir);
                    Console.WriteLine("MrgDir:: " + op.MergedXmlDir);
                    Console.WriteLine("--------------------------------------------------");
                    if (Mode.DIFF == mod)
                    {
                        new ModuleDiffer(op).RunAction();
                    }
                    else
                    {
                        new ModuleComparer(op).RunAction();
                    }
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine(rootDirPath + " does not exist!");
            }
        }
        static void MultiDiff(Opt op)
        {
            string rootDirPath = op.ReferencePaths[0];
            if (Directory.Exists(rootDirPath))
            {
                Dictionary<string, string> resLocs = new Dictionary<string, string>();
                DirectoryInfo rP = new DirectoryInfo(rootDirPath);
                Console.WriteLine("ResCmp::MultiDiff - " + rootDirPath);
                ScanResourceDirectories(rootDirPath, rP, resLocs, true);
                Console.WriteLine("ResCmp::Found " + resLocs.Count + " locations of resource folders.");
                string outDir = op.OutDir;
                Dictionary<string, string[]> rootParams = new Dictionary<string, string[]>();
                
                op.CopyParams(rootParams);
                foreach (string s in resLocs.Keys)
                {
                    string r = resLocs[s];
                    //string cL = r.Replace('\\', ':');
                    
                    op.Mode = Mode.DIFF; //this is a must
                    op.ReferencePaths = new string[] { s };
                   
                    op.IsSRStructure = false;  //should be set to false
                    op.Comment = r; // cL;

                    string lbl = op.ReferenceKey;
                    foreach (string k in rootParams.Keys) //rtm, hrp1
                    {
                        lbl = lbl + "-" + k;
                        string[] pR = rootParams[k];
                        string[] nR = new string[pR.Length];
                        for(int i=0; i<pR.Length; i++)  //root1;root2--> newPath1;newPath2
                            nR[i] = pR[i] + "\\" + r;
                        op.ChangeParam(k, nR);
                    }

                    string oD = outDir + "\\" + lbl + "\\" +  r;
                    op.OutDir = oD;
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine("ResCmp:: MultiDiff - " + s);
                    Console.WriteLine("OutDir:: " + op.OutDir);
                    Console.WriteLine("MrgDir:: " + op.MergedXmlDir);
                    Console.WriteLine("--------------------------------------------------");
                    new ModuleDiffer(op).RunAction();
                    //Console.WriteLine("OutDir(" + op.OutDir.Length + "): " + op.OutDir);
                    //Console.WriteLine("OutXml(" + op.OutXmlPath.Length + "): " + op.OutXmlPath);
                    //Console.WriteLine("MrgDir(" + op.MergedXmlDir.Length + "): " + op.MergedXmlDir);
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine(rootDirPath + " does not exist!");
            }

        }
        static void MultiCompare(Opt op)
        {
            string rootDirPath = op.ReferencePaths[0];
            if (Directory.Exists(rootDirPath ))
            {
                Dictionary<string, string> resLocs = new Dictionary<string, string>();
                DirectoryInfo rP = new DirectoryInfo(rootDirPath);
                Console.WriteLine("ResCmp::MultiCompare - " + rootDirPath);
                ScanResourceDirectories(rootDirPath, rP, resLocs, false);
                Console.WriteLine("ResCmp::Found " + resLocs.Count + " locations of resource folders.");
                string outDir = op.OutDir;
                foreach (string s in resLocs.Keys)
                {
                    string r = resLocs[s];
                    //string cL = r.Replace('\\', ':');
                    string oD = outDir + "\\" + r;
                    op.Mode = Mode.COMP; //this is a must
                    op.ReferencePaths = new string[] { s };
                    op.OutDir = oD;
                    op.IsSRStructure = true;  //should be set to true.
                    op.Comment = r; // cL;

                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine("ResCmp:: Comparing - " + s);
                    Console.WriteLine("OutDir:: " + op.OutDir);
                    Console.WriteLine("MrgDir:: " + op.MergedXmlDir);
                    Console.WriteLine("--------------------------------------------------");
                    new ModuleComparer(op).RunAction();
                    //Console.WriteLine("OutDir(" + op.OutDir.Length + "): " + op.OutDir);
                    //Console.WriteLine("OutXml(" + op.OutXmlPath.Length + "): " + op.OutXmlPath);
                    //Console.WriteLine("MrgDir(" + op.MergedXmlDir.Length + "): " + op.MergedXmlDir);
                    Console.WriteLine("--------------------------------------------------");                    
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine(rootDirPath + " does not exist!");
            }

        }
        static void ScanResourceDirectories(string rootDir, DirectoryInfo currDir, Dictionary<string, string> resLocs, bool bLeaf)
        {
            foreach (DirectoryInfo d in currDir.GetDirectories())
            {
                string pN = bLeaf ? d.FullName : d.Parent.FullName;
                if (IsCultureNameString(d.Name) && d.GetFiles("*.xml").Length > 0)
                {
                    if (!resLocs.ContainsKey(pN))
                    {
                        string rP = pN.Length > rootDir.Length ? pN.Substring(rootDir.Length+1) : pN;
                        resLocs.Add(pN, rP);
                    }
                }
                else
                {
                    ScanResourceDirectories(rootDir, d, resLocs, bLeaf);
                }
            }
        }
        static bool IsCultureNameString(string folderName)
        {
            try
            {
                CultureInfo.CreateSpecificCulture(folderName);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }



        //------------------------------------
        static void RunHander(string[] args)
        {
                Opt o = new Opt(args);
                AbstractHandler handler = null;
                switch (o.Mode)
                {
                    case Mode.DIFF:
                        handler = new ModuleDiffer(o);
                        break;
                    case Mode.COMP:
                        handler = new ModuleComparer(o);
                        break;
                    case Mode.AUTOCOMP:
                        //MultiCompare(o);
                        AutoProc(o, Mode.COMP);
                        break;
                    case Mode.AUTODIFF:
                        //MultiDiff(o);
                        AutoProc(o, Mode.DIFF);
                        break;
                    default:
                        PrintUsage();
                        break;
                }
                if (handler != null)
                    handler.RunAction();

        }



        /*********************************************
         * internal classes
         ***********************************************/
        struct FileSet
        {
            public string refFile;
            public List<string> subFiles;
        }
        //-------------------------------------
        internal class FileMap
        {
            private Dictionary<string, string> fileMaps = null;
            public void Add(string refName, string subName)
            {
                if (fileMaps == null) this.fileMaps = new Dictionary<string, string>();
                this.fileMaps.Add(refName, subName);
            }
            public Dictionary<string, string> FileMaps
            {
                get { return this.fileMaps; }
            }
            public string MappedName(string refName)
            {
                if (!this.fileMaps.ContainsKey(refName)) return refName;
                return this.fileMaps[refName];
            }
        }
        //-------------------------------------
        internal abstract class AbstractHandler
        {
            protected Dictionary<string, ModuleSet> moduleSets;
            private Dictionary<string, FileSet> fileList;
            //protected Dictionary<string, Block> missingBlocks;
            protected List<ResourceEntry> emptyValues;
            protected List<ResourceEntry> missingEntries;
            private List<DuplicateEntry> duplicateKeys;
            private List<DuplicateEntry> duplicateValues;
            protected XmlTextWriter resultWriter = null;
            protected Opt opt;
            protected ResGroupMgr grpResources;
            protected AbstractHandler(Opt opt)
            {
                this.opt = opt;
                this.moduleSets = new Dictionary<string, ModuleSet>();
                this.fileList = new Dictionary<string, FileSet>();
                //this.missingBlocks = new Dictionary<string, Block>();
                this.emptyValues = new List<ResourceEntry>();
                this.missingEntries = new List<ResourceEntry>();
                this.grpResources = new ResGroupMgr(this.opt);
                if (this.opt.IsParamAFile)
                {
                    CreateFileSet();
                }
                else
                {
                    ScanDirectory(this.opt.ReferenceKey, this.opt.ReferencePaths, true);

                    if (this.opt.ReferencePaths != null)
                    {
                        if (this.opt.IsSRStructure)
                        {
                            foreach (string dPath in this.opt.ReferencePaths)
                            {
                                //ref and sub are in the same level
                                if (this.opt.ReferenceKey != null && Directory.Exists(Path.Combine(dPath, this.opt.ReferenceKey)))
                                {
                                    ScanDirectory(this.opt.ReferenceKey, new string[] { Path.Combine(dPath, this.opt.ReferenceKey) }, true);
                                    foreach (string q in Directory.GetDirectories(dPath))
                                    {
                                        DirectoryInfo d = new DirectoryInfo(q);
                                        if (!d.Name.Equals(this.opt.ReferenceKey,StringComparison.CurrentCultureIgnoreCase) && IsValidCultureName(d.Name))
                                            ScanDirectory(d.Name, d.FullName, false);
                                    }
                                }
                                else
                                {
                                    foreach (string q in Directory.GetDirectories(dPath))
                                    {
                                        DirectoryInfo d = new DirectoryInfo(q);
                                        if (IsValidCultureName(d.Name))
                                            ScanDirectory(d.Name, d.FullName, false);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (string k in this.opt.Keys)
                            {
                                ScanDirectory(k, this.opt.Param(k), false);
                            }
                        }
                    }
                }

            }
            protected void CreateFileSet()
            {
                if (this.opt.Keys.Count > 0)
                {
                    string rFile = this.opt.ReferencePaths[0];
                    FileInfo f = new FileInfo(rFile);
                    WriteLog("ModuleSet({0}): {1} ", f.Name, rFile);
                    Module md = ProcXML(rFile, this.opt.ReferenceKey );
                    ModuleSet m = new ModuleSet(this.opt.ReferenceKey, md);

                    foreach (string sf in this.opt.Keys)
                    {
                        string cFile = this.opt.Param(sf)[0];
                        md = ProcXML(cFile, sf);
                        m.AddModule(sf, md);
                    }
                    this.moduleSets.Add(rFile, m);
                }
            }
            protected void ScanDirectory(string strKey, string[] dirPaths, bool bRef)
            {
                foreach (string p in dirPaths)
                    ScanDirectory(strKey, p, bRef);
            }
            protected void ScanDirectory(string strKey, string dirPath, bool bRef)
            {
                if (!Directory.Exists(dirPath))
                {
                    Console.WriteLine("Directory {0} does not exist!", dirPath);
                    return;
                }
                foreach (string file in Directory.GetFiles(dirPath, "*.xml"))
                {
                    FileInfo f = new FileInfo(file);
                    string fName = f.Name.Replace("resources.", "").ToLower();
                    string mFile = this.opt.GetMappedFile(fName.ToLower());
                    

                    if (!this.fileList.ContainsKey(fName))
                    {
                        if (bRef)
                        {
                            WriteLog("NewFileSet({0}): {1} ", fName, file);
                            FileSet s = new FileSet();
                            s.refFile = file;
                            s.subFiles = new List<string>();
                            this.fileList.Add(fName, s);
                            Module md = ProcXML(file, strKey);
                            if(md!=null){
                                ModuleSet m = new ModuleSet(strKey, md);
                                this.moduleSets.Add(fName, m);
                            }
                        }
                        else
                        {
                            if (mFile != null)
                            {
                                string mmFile = GetMappedFileKey(mFile);
                                if (mmFile !=null)
                                {
                                    Module md = ProcXML(file, strKey);
                                    WriteLog("MappedFileFound: {0} -> {1} ", fName, mmFile);


                                    if (md == null)
                                    {
                                        WriteLog("NoResources: {0} doesn't have the resource entries. ", fName);
                                    }
                                    else
                                    {
                                        this.fileList[mmFile].subFiles.Add(file);
                                        this.moduleSets[mmFile].AddModule(strKey, md);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Module md = ProcXML(file, strKey);
                        if(md!=null){
                            WriteLog("AddingSubFile({0}): {1} ", fName, file);
                            this.fileList[fName].subFiles.Add(file);
                            this.moduleSets[fName].AddModule(strKey, md);
                        }
                    }
                }
                if (this.opt.IsRecursive)
                {
                    foreach (string dir in Directory.GetDirectories(dirPath))
                    {
                        ScanDirectory(strKey, dir, bRef);
                    }
                }
            }
            private string GetMappedFileKey(string mFile)
            {
                if (this.fileList.ContainsKey(mFile)) return mFile;
                foreach (string s in this.fileList.Keys)
                {
                    if (s.Equals(mFile, StringComparison.CurrentCultureIgnoreCase)) return s;
                }
                return null;
            }

            protected void WriteLog(string text, params object[] p)
            {
                Console.WriteLine(text, p);
            }
            protected string GetLangId(string langCode)
            {
                int test;
                if (Int32.TryParse(langCode, out test)) return langCode;
                return CultureInfo.CreateSpecificCulture(langCode).LCID.ToString();
            }
            protected bool IsValidCultureName(string langCode)
            {
                try
                {
                    GetLangId(langCode);
                    return true;
                }
                catch (Exception e)
                {
                    //WriteLog("Error: {0}", e.Message);
                    return false;
                }
            }

            protected Module ProcXML(string xmlFile, string resLang)
            {
                Module m = null;
                try
                {
                    XmlDocument oXml = new XmlDocument();
                    oXml.Load(xmlFile);
                    XmlNode mNode = oXml.DocumentElement;
                    //if("xml".Equals(mNode.Name))

                    if ("module".Equals(mNode.Name))
                    {
                        string name = mNode.Attributes["name"].Value;
                        string path = mNode.Attributes["path"].Value;
                        m = new Module(name, path);
                        foreach (XmlNode xNode in mNode.ChildNodes)
                        {
                            string block = xNode.Attributes["block"].Value;
                            XmlAttribute att = xNode.Attributes["lang"];

                            string lang = String.Empty;
                            if (att == null && IsValidCultureName(resLang))
                            {
                                lang = GetLangId(resLang);
                                WriteLog("LangString to LangId: {0} -> {1}", resLang, lang);
                            }
                            else
                            {
                                lang = att != null ? att.Value : DEFLANG;
                            }


                            att = xNode.Attributes["size"];
                            string size = att == null ? null : att.Value;
                            Block b = new Block(xNode.Name, block, lang, size);
                            m.AddBlock(b);
                            foreach (XmlNode o in xNode.ChildNodes)
                            {
                                string key = o.Attributes["key"].Value;
                                string val = o.InnerText;
                                if (!String.Empty.Equals(val) && !this.opt.IsInNoCompareKeys(key) && b.ValueExists(val))
                                    AddDuplicateValue(new ResourceEntry(name, xNode.Name, resLang, block, b.GetKey(val), val),
                                    new ResourceEntry(name, xNode.Name, resLang, block, key, val));
                                if (b.KeyExists(key))
                                {
                                    AddDuplicateKey(new ResourceEntry(name, xNode.Name, resLang, block, key, b.GetText(key)),
                                    new ResourceEntry(name, xNode.Name, resLang, block, key, val));
                                }
                                else
                                {
                                    b.AddText(key, val);
                                }


                            }
                        }
                    }
                    oXml = null;
                }
                catch (Exception e)
                {
                    WriteLog("XMLProcessingError: {0}", xmlFile);
                    WriteLog("ExceptionDetails: {0}", e.StackTrace);
                }
                return m;
            }
            protected void PrepareResultWriter()
            {
                if (this.resultWriter == null)
                {
                    this.resultWriter = new XmlTextWriter(this.opt.OutXmlPath, Encoding.UTF8);
                    this.resultWriter.Formatting = Formatting.Indented;
                    //this.resultWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");

                    this.resultWriter.WriteStartDocument();

                    if (opt.ResultXSLFile != null)
                    {
                        string PItext = "type='text/xsl' href='" + opt.ResultXSLFile +"'";
                        this.resultWriter.WriteProcessingInstruction("xml-stylesheet", PItext);
                    }


                    this.resultWriter.WriteStartElement("rescmp");
                    this.resultWriter.WriteAttributeString("execDate", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
                    if (this.opt.Comment != null)
                    {
                        this.resultWriter.WriteStartElement("comments");
                        this.resultWriter.WriteString(this.opt.Comment);
                        this.resultWriter.WriteEndElement();
                    }
                }
            }
            protected void CloseResultWriter()
            {
                this.resultWriter.WriteEndElement(); //rescmp
                this.resultWriter.Close();
            }
            protected void CheckMissingModules()
            {
                //remove module set which don't have sub modules
                this.resultWriter.WriteStartElement("unlocalizedModules");
                foreach (string y in new List<string>(this.moduleSets.Keys))
                {
                    ModuleSet ms = this.moduleSets[y];
                    if (ms.SubModulesCount == 0)
                    {
                        if (ms.RefModule.Types.Count == 0)
                        {
                            //no resources 
                            WriteLog("NoResourceFound: Removing from list. ({0})", ms.Name);
                        }
                        else
                        {
                            WriteLog("UnlocalizedResourceModule: {0}", ms.Name);
                            this.resultWriter.WriteStartElement("module");
                            this.resultWriter.WriteAttributeString("name", ms.RefModule.Name);
                            this.resultWriter.WriteAttributeString("types", ms.RefModule.Info());
                            this.resultWriter.WriteAttributeString("path", ms.RefModule.Path);
                            
                            this.resultWriter.WriteEndElement();
                        }
                        this.moduleSets.Remove(y);
                    }
                }
                this.resultWriter.WriteEndElement(); //unlocalizedModules
                WriteLog("ModuleSetCount: {0}", this.moduleSets.Count);
            }

            protected void WriteDuplicateEntries()
            {
                if (this.duplicateKeys != null)
                {

                    this.resultWriter.WriteStartElement("duplicateKeys");
                    foreach (DuplicateEntry r in this.duplicateKeys)
                    {

                        this.resultWriter.WriteStartElement("module");
                        this.resultWriter.WriteAttributeString("name", r.entryOne.moduleName);
                        this.resultWriter.WriteAttributeString("lang", r.entryOne.language);
                        this.resultWriter.WriteAttributeString("block", r.entryOne.blockName);
                        this.resultWriter.WriteAttributeString("type", r.entryOne.resourceType);
                        this.resultWriter.WriteStartElement("text");
                        this.resultWriter.WriteAttributeString("key", r.entryOne.resourceKey);
                        this.resultWriter.WriteString(r.entryOne.resourceValue);
                        this.resultWriter.WriteEndElement();
                        this.resultWriter.WriteStartElement("text");
                        this.resultWriter.WriteAttributeString("key", r.entryTwo.resourceKey);
                        this.resultWriter.WriteString(r.entryTwo.resourceValue);
                        this.resultWriter.WriteEndElement();
                        this.resultWriter.WriteEndElement();
                    }
                    this.resultWriter.WriteEndElement(); //duplicateKeys
                }
                if (this.duplicateValues != null)
                {

                    this.resultWriter.WriteStartElement("duplicateValues");
                    foreach (DuplicateEntry r in this.duplicateValues)
                    {

                        this.resultWriter.WriteStartElement("module");
                        this.resultWriter.WriteAttributeString("name", r.entryOne.moduleName);
                        this.resultWriter.WriteAttributeString("lang", r.entryOne.language);
                        this.resultWriter.WriteAttributeString("block", r.entryOne.blockName);
                        this.resultWriter.WriteAttributeString("type", r.entryOne.resourceType);
                        this.resultWriter.WriteStartElement("text");
                        this.resultWriter.WriteAttributeString("key", r.entryOne.resourceKey);
                        this.resultWriter.WriteString(r.entryOne.resourceValue);
                        this.resultWriter.WriteEndElement();
                        this.resultWriter.WriteStartElement("text");
                        this.resultWriter.WriteAttributeString("key", r.entryTwo.resourceKey);
                        this.resultWriter.WriteString(r.entryTwo.resourceValue);
                        this.resultWriter.WriteEndElement();
                        this.resultWriter.WriteEndElement();
                    }
                    this.resultWriter.WriteEndElement(); //duplicateValues
                }
            }
            protected void CheckLeftOverEntries(ModuleSet modSet, bool bRef)
            {
                if (bRef)
                {
                    CheckLeftOverBlockEntries(modSet.RefModule, modSet.RefModuleLang, modSet);
                }
                else
                {
                    foreach (string subModKey in modSet.Keys)
                        CheckLeftOverBlockEntries(modSet.GetModule(subModKey), subModKey,null);
                }
            }


            protected void MergeAndCheckUnlocEntries(ModuleSet modSet, XmlWriter writer)
            {
                Dictionary<string, string> blockCache = new Dictionary<string, string>();
                List<string> unlocLangs = new List<string>();
                writer.WriteStartElement("module");
                writer.WriteAttributeString("name", modSet.Name);
                string mF = this.opt.GetMappedFile(modSet.Name.ToLower() + ".xml");
                if (mF != null) writer.WriteAttributeString("mappedFile", mF.Replace(".xml", ""));

                //moduleset
                writer.WriteStartElement("files");
                writer.WriteStartElement("file");
                writer.WriteAttributeString("lang", this.opt.ReferenceKey);
                writer.WriteAttributeString("path", modSet.RefModule.Path);
                writer.WriteEndElement();
                foreach (string m in modSet.Keys)
                {
                    writer.WriteStartElement("file");
                    writer.WriteAttributeString("lang", m);
                    writer.WriteAttributeString("path", modSet.GetModule(m).Path);
                    writer.WriteEndElement(); //file
                }
                writer.WriteEndElement(); //files

                //resources
                writer.WriteStartElement("resources");
                foreach (string type in modSet.RefModule.Types)
                {
                    string refLangId = GetLangId(modSet.RefModuleLang);
                    //modSet.RefModule.Ha
                    ICollection<string> blockIds = modSet.RefModule.GetKeys(type, refLangId);
                    if (blockIds.Count == 00 && this.opt.HasAltCode(modSet.RefModuleLang.ToLower()))
                    {
                        string nrLang = this.opt.GetAltCode(modSet.RefModuleLang.ToLower());
                        WriteLog("GettingAltLangCode({0}): {1}->{2}", modSet.RefModuleLang, refLangId, nrLang);
                        refLangId = nrLang;                     
                        blockIds = modSet.RefModule.GetKeys(type, refLangId);

                    }
                    foreach (string blockId in blockIds)
                    {
                        writer.WriteStartElement(type);
                        writer.WriteAttributeString("block", blockId);
                        Block refBlock = modSet.RefModule.GetBlock(type, refLangId, blockId);

                        foreach (string uBase in new List<string>(refBlock.Keys))
                        {
                            writer.WriteStartElement("text");
                            writer.WriteAttributeString("key", uBase);
                            writer.WriteStartElement(this.opt.ReferenceKey);
                            string refVal = refBlock.GetText(uBase);
                            int phCount = refVal.Split('%').Length - 1;
                            writer.WriteString(refVal);
                            writer.WriteEndElement();//ref
                            bool bUnloc = false;
                            blockCache.Clear();
                            unlocLangs.Clear();
                            blockCache.Add(this.opt.ReferenceKey, refVal);

                            if (string.Empty.Equals(refVal))
                            {
                                this.emptyValues.Add(new ResourceEntry(modSet.RefModule.Name, type, this.opt.ReferenceKey, blockId, uBase, string.Empty));
                            }

                            GroupedRes r = this.grpResources.Add(modSet.RefModule.Name, this.opt.ReferenceKey, blockId, uBase, refVal);

                            //sub modules
                            int sCount = modSet.SubModulesCount;
                            foreach (string modLang in modSet.Keys)
                            {

                                string lang2Id = GetLangId(modLang);
                                Module subMod = modSet.GetModule(modLang);
                                Block subBlock = subMod.GetBlock(type, lang2Id, blockId);
                                //resource block not found in localized modules
                                //if (subBlock == null && modLang.ToLower().StartsWith("es"))
                                if (subBlock == null && this.opt.HasAltCode(modLang.ToLower())){
                                    string snrLang = this.opt.GetAltCode(modLang.ToLower());
                                    WriteLog("GettingAltLangCode({0}): {1}->{2}", modLang, lang2Id, snrLang);
                                    subBlock = subMod.GetBlock(type, snrLang, blockId);
                                }

                                if (subBlock == null)
                                {
                                    WriteLog("MissingBlock[{0}]: module={1} type={2} block={3}", modLang, modSet.Name, type, blockId);
                                    //this.unlocalizedBlocks.Add 
                                }
                                else
                                {
                                    string locVal = subBlock.GetText(uBase);
                                    //entry is not localized in other platform
                                    if (locVal == null)
                                    {
                                        WriteLog("MissingEntry[{0}]: module={1} type={2} block={3} key={4}", modLang, modSet.Name, type, blockId, uBase);
                                        this.missingEntries.Add(new ResourceEntry(subMod.Name, type, modSet.RefModuleLang, blockId, uBase, refVal, modLang));
                                    }
                                    else
                                    {
                                        writer.WriteStartElement(modLang);
                                        //if (modLang.ToLower().StartsWith("es") && locVal == null)
                                        if (locVal == null && this.opt.HasAltCode(modLang.ToLower() ))
                                            locVal = modSet.GetModule(modLang).GetText(type, this.opt.GetAltCode(modLang.ToLower()), blockId, uBase);
                                        
                                        
                                        //check if refString == to cmpString
                                        if (!refVal.Equals("") )
                                        {
                                            if (refVal.Equals(locVal))
                                            {
                                                writer.WriteAttributeString("unLoc", "1");
                                                bUnloc = true;
                                                if (!unlocLangs.Contains(modLang)) unlocLangs.Add(modLang);
                                            }
                                            else
                                            {
                                                //strings were not the same as the refVal, let's count the number of %x if there are
                                                if (phCount > -1)
                                                {
                                                    int cmpPhCount = locVal.Split('%').Length - 1;
                                                    if (phCount != cmpPhCount)
                                                    {
                                                        writer.WriteAttributeString("phDif", "1");
                                                    }
                                                }
                                            }
                                        }
                                        //TBD:: need to report when there are duplicates
                                        if (!blockCache.ContainsKey(modLang))
                                        {
                                            blockCache.Add(modLang, locVal);
                                        }
                                        if(r!=null)
                                            r.AddSubGroup(this.grpResources.Add(subMod.Name, modLang, blockId, uBase, locVal));
                                        writer.WriteString(locVal);
                                        writer.WriteEndElement();//ref
                                        subBlock.RemoveText(uBase);
                                        sCount--;
                                    }
                                    if (string.Empty.Equals(locVal))
                                    {
                                        this.emptyValues.Add(new ResourceEntry(subMod.Name, type, modLang, blockId, uBase, string.Empty));
                                    }
                                }
                            }

                            if (bUnloc)
                            {
                                WriteDiffEntry(modSet.Name, type, blockId, uBase, blockCache, unlocLangs, "unLoc");
                            }
                            if (sCount == 0)
                            {
                                refBlock.RemoveText(uBase);
                            }
                            writer.WriteEndElement(); //text
                        }
                        writer.WriteEndElement(); //type/block
                    }
                }
                writer.WriteEndElement(); //resources
                writer.WriteEndElement(); //module
            }
            //2 lang files diff
            protected void CompareAndCheckEntries(ModuleSet modSet, XmlWriter writer)
            {
                Dictionary<string, string> blockCache = new Dictionary<string, string>();
                List<string> unlocLangs = new List<string>();
                writer.WriteStartElement("module");
                writer.WriteAttributeString("name", modSet.Name);

                //moduleset
                writer.WriteStartElement("files");
                writer.WriteStartElement(this.opt.ReferenceKey);
                writer.WriteAttributeString("path", modSet.RefModule.Path);
                writer.WriteEndElement();
                foreach (string m in modSet.Keys)
                {
                    writer.WriteStartElement(m);
                    writer.WriteAttributeString("path", modSet.GetModule(m).Path);
                    writer.WriteEndElement(); //file
                }
                writer.WriteEndElement(); //files

                //resources
                writer.WriteStartElement("resources");
                foreach (string type in modSet.RefModule.Types)
                {
                    ICollection<string> resLangs = modSet.RefModule.GetKeys(type);
                    foreach (string resLang in resLangs)
                    {
                        ICollection<string> blockIds = modSet.RefModule.GetKeys(type, resLang);
                        foreach (string blockId in blockIds)
                        {
                            writer.WriteStartElement(type);
                            writer.WriteAttributeString("block", blockId);
                            writer.WriteAttributeString("lang", resLang);
                            Block refBlock = modSet.RefModule.GetBlock(type, resLang, blockId);

                            foreach (string uBase in new List<string>(refBlock.Keys))
                            {
                                writer.WriteStartElement("text");
                                writer.WriteAttributeString("key", uBase);
                                writer.WriteStartElement(this.opt.ReferenceKey);
                                string refVal = refBlock.GetText(uBase);
                                writer.WriteString(refVal);
                                writer.WriteEndElement();//ref
                                bool bChanged = false;
                                blockCache.Clear();
                                unlocLangs.Clear();
                                blockCache.Add(this.opt.ReferenceKey, refVal);

                                if (string.Empty.Equals(refVal))
                                {
                                    this.emptyValues.Add(new ResourceEntry(modSet.RefModule.Name, type, this.opt.ReferenceKey, blockId, uBase, string.Empty));
                                }
                                GroupedRes r = this.grpResources.Add(modSet.RefModule.Name, this.opt.ReferenceKey, blockId, uBase, refVal);

                                //sub modules
                                int sCount = modSet.SubModulesCount;
                                foreach (string modLang in modSet.Keys)
                                {
                                    writer.WriteStartElement(modLang);
                                    Module subMod = modSet.GetModule(modLang);
                                    Block subBlock = subMod.GetBlock(type, resLang, blockId);
                                    //if (subBlock == null && modLang.ToLower().StartsWith("es"))
                                    if (subBlock == null && this.opt.HasAltCode(modLang.ToLower()))
                                        subBlock = subMod.GetBlock(type, this.opt.GetAltCode(modLang.ToLower()), blockId);
                                    //resource block not found in newer version
                                    string locVal = string.Empty;
                                    if (subBlock == null)
                                    {
                                        WriteLog("MissingBlock[{0}]: module={1} type={2} block={3}", modLang, modSet.Name, type, blockId);
                                        //this.unlocalizedBlocks.Add 
                                    }
                                    else
                                    {
                                        locVal = subBlock.GetText(uBase);

                                        if (locVal == null)
                                        {
                                            WriteLog("MissingEntry[{0}]: module={1} type={2} block={3} key={4}", modLang, modSet.Name, type, blockId, uBase);
                                        }
                                        //entry was changed on the new versions
                                        else
                                        {
                                            if (!refVal.Equals("") && !refVal.Equals(locVal))
                                            {
                                                writer.WriteAttributeString("changed", "1");
                                                bChanged = true;
                                                if (!unlocLangs.Contains(modLang)) unlocLangs.Add(modLang);
                                            }
                                            //TBD:: need to report when there are duplicates
                                            if (!blockCache.ContainsKey(modLang))
                                            {
                                                blockCache.Add(modLang, locVal);
                                            }
                                            if (r != null)
                                                r.AddSubGroup(this.grpResources.Add(subMod.Name, modLang, blockId, uBase, locVal));
                                            subBlock.RemoveText(uBase);
                                            sCount--;
                                        }
                                    }
                                    if (string.Empty.Equals(locVal))
                                    {
                                        this.emptyValues.Add(new ResourceEntry(subMod.Name, type, modLang, blockId, uBase, string.Empty));
                                    }
                                    writer.WriteString(locVal);
                                    writer.WriteEndElement();//ref
                                }

                                if (bChanged)
                                {
                                    WriteDiffEntry(modSet.Name, type, blockId, uBase, blockCache, unlocLangs, "changed");
                                }
                                if (sCount == 0)
                                {
                                    refBlock.RemoveText(uBase);
                                }
                                writer.WriteEndElement(); //text
                            }
                            writer.WriteEndElement(); //type/block
                        }
                    }
                }
                writer.WriteEndElement(); //resources
                writer.WriteEndElement(); //module
            }
            private void WriteDiffEntry(string moduleName, string type, string blockId, string uBase, Dictionary<string, string> texts, List<string> langs, string attStr)
            {
                this.resultWriter.WriteStartElement("module");
                this.resultWriter.WriteAttributeString("name", moduleName);
                this.resultWriter.WriteStartElement(type);
                this.resultWriter.WriteAttributeString("block", blockId);
                this.resultWriter.WriteAttributeString("key", uBase);
                foreach (string s in texts.Keys)
                {
                    this.resultWriter.WriteStartElement(s);
                    if (langs.Contains(s)) this.resultWriter.WriteAttributeString(attStr, "1");
                    this.resultWriter.WriteString(texts[s]);
                    this.resultWriter.WriteEndElement();//s
                }
                this.resultWriter.WriteEndElement();//type
                this.resultWriter.WriteEndElement(); //module
            }
            private void AddDuplicateKey(ResourceEntry r1, ResourceEntry r2)
            {
                if (this.duplicateKeys == null) this.duplicateKeys = new List<DuplicateEntry>();
                this.duplicateKeys.Add(new DuplicateEntry(r1, r2));
            }
            private void AddDuplicateValue(ResourceEntry r1, ResourceEntry r2)
            {
                if (this.duplicateValues == null) this.duplicateValues = new List<DuplicateEntry>();
                this.duplicateValues.Add(new DuplicateEntry(r1, r2));
            }

            protected void WriteEmptyValues()
            {
                this.resultWriter.WriteStartElement("emptyValues");
                foreach (ResourceEntry r in this.emptyValues)
                {

                    this.resultWriter.WriteStartElement("module");
                    this.resultWriter.WriteAttributeString("name", r.moduleName);
                    this.resultWriter.WriteAttributeString("lang", r.language);
                    this.resultWriter.WriteAttributeString("block", r.blockName);
                    this.resultWriter.WriteAttributeString("type", r.resourceType);
                    this.resultWriter.WriteAttributeString("key", r.resourceKey);
                    this.resultWriter.WriteEndElement();
                }
                this.resultWriter.WriteEndElement(); //emptyValues
            }
            protected void WriteMissingEntriesDetails()
            {
                this.resultWriter.WriteStartElement("missingEntries");
                string currMod = null;
                string currBlock = null;
                bool bMod = false;
                bool bBlk = false;
                foreach (ResourceEntry r in this.missingEntries)
                {
                    if (!r.moduleName.Equals(currMod))
                    {
                        if (bMod)
                        {
                            if (bBlk)
                            {
                                this.resultWriter.WriteEndElement(); //block
                                bBlk = false;
                            }
                            this.resultWriter.WriteEndElement();
                        }

                            this.resultWriter.WriteStartElement("module");
                            this.resultWriter.WriteAttributeString("name", r.moduleName);
                            this.resultWriter.WriteAttributeString("lang", r.language);
                            currMod = r.moduleName;

                        bMod = true;
                    }
                    if (!r.blockName.Equals(currBlock))
                    {
                        if (bBlk)
                        {
                            this.resultWriter.WriteEndElement();
                        }                     
                            this.resultWriter.WriteStartElement(r.resourceType);
                            this.resultWriter.WriteAttributeString("block", r.blockName);
                            currBlock = r.blockName;
                        
                        bBlk = true;
                    }

                    this.resultWriter.WriteStartElement("text");
                    this.resultWriter.WriteAttributeString("missingIn", r.otherData); 
                    this.resultWriter.WriteAttributeString("key", r.resourceKey);
                    this.resultWriter.WriteString(r.resourceValue );
                    this.resultWriter.WriteEndElement();
                }
                if (bMod)
                {
                    if (bBlk) this.resultWriter.WriteEndElement(); //block
                    this.resultWriter.WriteEndElement(); //module
                }
                this.resultWriter.WriteEndElement(); //missingEntries
            }
            public abstract bool RunAction();
            protected abstract void CheckLeftOverBlockEntries(Module mod, string modLang, ModuleSet modSet);
        }
        //-------------------------------------
        internal class ModuleComparer : AbstractHandler
        {


            public ModuleComparer(Opt opt)
                : base(opt)
            {

            }
            public override bool RunAction()
            {
                string mergeXMLDir = Directory.CreateDirectory(this.opt.MergedXmlDir).FullName;
                PrepareResultWriter();
                //write unlocalized modules
                CheckMissingModules();
                //merge & unlocalizedEntries
                this.resultWriter.WriteStartElement("unlocalizedEntries");
                foreach (string y in this.moduleSets.Keys)
                {
                    ModuleSet ms = this.moduleSets[y];
                    if (ms.SubModulesCount > 0)
                    {
                        using (XmlTextWriter w = new XmlTextWriter(mergeXMLDir + "\\" + ms.Name + ".xml", Encoding.UTF8))
                        {
                            w.Formatting = Formatting.Indented;

                            //w.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                            w.WriteStartDocument();
                            if (opt.MergedXSLFile != null)
                            {
                                string PItext = "type='text/xsl' href='" + opt.MergedXSLFile + "'";
                                w.WriteProcessingInstruction("xml-stylesheet", PItext);
                            }
                            MergeAndCheckUnlocEntries(ms, w);
                            w.WriteEndDocument();
                            w.Close();
                        }
                    }
                }
                this.resultWriter.WriteEndElement(); //results/,ergedValues
                //verification

                WriteMissingEntriesDetails();
                
                if (this.moduleSets.Count > 0)
                {
                    /*
                    this.resultWriter.WriteStartElement("missingEntries");
                    foreach (string y in this.moduleSets.Keys)
                    {
                        CheckLeftOverEntries(this.moduleSets[y], true);
                    }
                    this.resultWriter.WriteEndElement(); //extraEntries
                    */
                    
                    this.resultWriter.WriteStartElement("newEntries");
                    foreach (string y in this.moduleSets.Keys)
                    {
                        CheckLeftOverEntries(this.moduleSets[y], false);
                    }

                    this.resultWriter.WriteEndElement(); //extraEntries
                }
                
                
                if(!this.opt.IsSuppressDupValues) WriteDuplicateEntries();
                if(!this.opt.IsSuppressEmpValues) WriteEmptyValues();
                if(!this.opt.IsSuppressGrpItems) this.grpResources.WriteToXML(this.resultWriter);
                this.CloseResultWriter();
                return true;
            }
            protected override void CheckLeftOverBlockEntries(Module mod, string modLang, ModuleSet modSet)
            {
                foreach (string type in mod.Types)
                {

                    string langId = IsValidCultureName(modLang) ? GetLangId(modLang) : DEFLANG;
                    ICollection<string> blockIds = mod.GetKeys(type, langId);
                    foreach (string blockId in blockIds)
                    {
                        Block blck = mod.GetBlock(type, langId, blockId);
                        if (blck.Count > 0)
                        {
                            this.resultWriter.WriteStartElement("module");
                            this.resultWriter.WriteAttributeString("name", mod.Name);
                            this.resultWriter.WriteAttributeString("lang", modLang);
                            if (modSet != null && modSet.Keys.Count > 0)
                            {
                                string[] mL = new String[modSet.Keys.Count];
                                modSet.Keys.CopyTo(mL, 0);
                                this.resultWriter.WriteAttributeString("missingLang", String.Join(",", mL));
                            }
                            this.resultWriter.WriteStartElement(type);
                            this.resultWriter.WriteAttributeString("block", blockId);
                            foreach (string uBase in blck.Keys)
                            {
                                string rv = blck.GetText(uBase);
                                this.resultWriter.WriteStartElement("text");
                                this.resultWriter.WriteAttributeString("key", uBase);
                                this.resultWriter.WriteString(rv);
                                this.resultWriter.WriteEndElement();

                                int lip = uBase.LastIndexOf('.');
                                if (lip > 1)
                                {
                                    string pName = uBase.Substring(lip + 1).ToLower();
                                    if (this.opt.IsInGroupedNames(pName))
                                    {
                                        string kName = uBase.Substring(0, lip);
                                        string bid = blockId;
                                        if (blockId.ToLower().EndsWith("." + modLang.ToLower()))
                                            bid = blockId.Substring(0, blockId.Length - (modLang.Length + 1));
                                        GroupedRes gr = this.grpResources.Get(mod.Name, modLang, bid, kName);
                                        if (gr != null)
                                            this.grpResources.Add(mod.Name, modLang, bid, uBase, rv);
                                    }
                                }
                            }
                            this.resultWriter.WriteEndElement();//type
                            this.resultWriter.WriteEndElement(); //module
                        }
                    }
                }
            }


        }
        //-------------------------------------
        internal class ModuleDiffer : AbstractHandler
        {
            public ModuleDiffer(Opt opt)
                : base(opt)
            {

            }
            public override bool RunAction()
            {
                string mergeXMLDir = Directory.CreateDirectory(this.opt.MergedXmlDir).FullName;
                PrepareResultWriter();
                //merge
                this.resultWriter.WriteStartElement("changedEntries");
                foreach (string y in this.moduleSets.Keys)
                {
                    ModuleSet ms = this.moduleSets[y];
                    if (ms.SubModulesCount > 0)
                    {
                        using (XmlTextWriter w = new XmlTextWriter(mergeXMLDir + "\\" + ms.Name + ".xml", Encoding.UTF8))
                        {
                            w.Formatting = Formatting.Indented;

                            //w.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                            w.WriteStartDocument();
                            if (opt.MergedXSLFile != null)
                            {
                                string PItext = "type='text/xsl' href='" + opt.MergedXSLFile + "'";
                                w.WriteProcessingInstruction("xml-stylesheet", PItext);
                            }
                            CompareAndCheckEntries(ms, w);
                            w.WriteEndDocument();
                            w.Close();
                        }
                    }
                }
                this.resultWriter.WriteEndElement(); //results/,ergedValues
                //verification

                WriteMissingEntriesDetails();
                if (this.moduleSets.Count > 0)
                {
                    /*
                    this.resultWriter.WriteStartElement("missingEntries");
                    foreach (string y in this.moduleSets.Keys)
                    {
                        CheckLeftOverEntries(this.moduleSets[y], true);
                    }
                    this.resultWriter.WriteEndElement(); //extraEntries
                    */
                      
                    this.resultWriter.WriteStartElement("newEntries");
                    foreach (string y in this.moduleSets.Keys)
                    {
                        CheckLeftOverEntries(this.moduleSets[y], false);
                    }

                    this.resultWriter.WriteEndElement(); //extraEntries
                }
                if (!this.opt.IsSuppressDupValues) WriteDuplicateEntries();
                if (!this.opt.IsSuppressEmpValues) WriteEmptyValues();
                if (!this.opt.IsSuppressGrpItems) this.grpResources.WriteToXML(this.resultWriter);
                this.CloseResultWriter();
                return true;
            }
            protected override void CheckLeftOverBlockEntries(Module mod, string modLang, ModuleSet modSet)
            {
                foreach (string type in mod.Types)
                {
                    ICollection<string> langIds = mod.GetBlockKeys(type);
                    foreach (string langId in langIds)
                    {
                        //string langId = IsValidCultureName(modLang) ? GetLangId(modLang) : DEFLANG;
                        ICollection<string> blockIds = mod.GetKeys(type, langId);

                        foreach (string blockId in blockIds)
                        {
                            Block blck = mod.GetBlock(type, langId, blockId);
                            if (blck.Count > 0)
                            {
                                this.resultWriter.WriteStartElement("module");
                                this.resultWriter.WriteAttributeString("name", mod.Name);
                                this.resultWriter.WriteAttributeString("lang", modLang);
                                if (modSet != null && modSet.Keys.Count > 0)
                                {
                                    string[] mL = new String[modSet.Keys.Count];
                                    modSet.Keys.CopyTo(mL, 0);                                    
                                    this.resultWriter.WriteAttributeString("missingLang", String.Join(",", mL));
                                }
                                this.resultWriter.WriteStartElement(type);
                                this.resultWriter.WriteAttributeString("block", blockId);
                                foreach (string uBase in blck.Keys)
                                {
                                    string rv = blck.GetText(uBase);
                                    this.resultWriter.WriteStartElement("text");
                                    this.resultWriter.WriteAttributeString("key", uBase);
                                    this.resultWriter.WriteString(rv);
                                    this.resultWriter.WriteEndElement();

                                    int lip = uBase.LastIndexOf('.');
                                    if (lip > 1)
                                    {
                                        string pName = uBase.Substring(lip + 1).ToLower();
                                        if (this.opt.IsInGroupedNames(pName))
                                        {
                                            string kName = uBase.Substring(0, lip);
                                            string bid = blockId;
                                            if (blockId.ToLower().EndsWith("." + modLang.ToLower()))
                                                bid = blockId.Substring(0, blockId.Length - (modLang.Length + 1));
                                            GroupedRes gr = this.grpResources.Get(mod.Name, modLang, bid, kName);
                                            if (gr != null)
                                                this.grpResources.Add(mod.Name, modLang, bid, uBase, rv);
                                        }
                                    }
                                }
                                this.resultWriter.WriteEndElement();//type
                                this.resultWriter.WriteEndElement(); //module
                            }
                        }
                    }
                }
            }
        }

        //-------------------------------------
        internal class ModuleSet
        {
            private string name;
            private Module refMod;
            private string refModLang;
            private Dictionary<string, Module> subMods;
            public ModuleSet(string lang, Module refMod)
            {
                this.refModLang = lang;
                this.name = refMod.Name;
                this.refMod = refMod;
                this.subMods = new Dictionary<string, Module>();
            }
            public void AddModule(string lang, Module mod)
            {
                if (this.subMods.ContainsKey(lang))
                {
                    Console.WriteLine("FileIgnored: {0}", mod.Path);
                    return;
                }
                this.subMods.Add(lang, mod);
            }
            public ICollection<string> Keys
            {
                get { return this.subMods.Keys; }
            }
            public Module GetModule(string lang)
            {
                return this.subMods[lang];
            }
            public string Name
            {
                get { return name; }
            }
            public string RefModuleLang
            {
                get { return refModLang; }
            }
            public Module RefModule
            {
                get { return refMod; }
            }
            public int SubModulesCount
            {
                get { return this.subMods.Count; }
            }
            public string[] GetMissingLang(string type, string blkLang, string blockId, string key)
            {
                List<string> mL = new List<string>();
                foreach (string modLang in this.subMods.Keys)
                {
                    Module mod = this.subMods[modLang];
                    if (!mod.HasTextKey(type, blkLang, blockId, key)) mL.Add(modLang);
                }
                return mL.ToArray();
            }

        }
        //-------------------------------------
        internal class Module
        {
            private const string DEFSTR = "$DEF$";
            private string name;
            private string path;
            //type, blockid, langid
            private Dictionary<string, Dictionary<string, Dictionary<string, Block>>> blocks;
            public Module(string name, string path)
            {
                this.name = name;
                this.path = path;
                this.blocks = new Dictionary<string, Dictionary<string, Dictionary<string, Block>>>();
            }
            public void AddBlock(Block block)
            {
                Dictionary<string, Block> b = GetDic(block.Type, block.Lang);

                b.Add(RemoveLangPrefix(block.Name), block);
            }
            private string RemoveLangPrefix(string name)
            {
                string r = name;
                int x = name.LastIndexOf('.') ;
                if (x > 1)
                {                    
                    string lN = name.Substring(x + 1).ToLower();
                    if(IsCultureNameString(lN))
                    {
                        r = name.Substring(0, x);
                    }
                }
                return r.ToLower();
            }
            private Dictionary<string, Dictionary<string, Block>> GetDic(string type)
            {
                if (this.blocks.ContainsKey(type))
                    return this.blocks[type];
                Dictionary<string, Dictionary<string, Block>> d = new Dictionary<string, Dictionary<string, Block>>();
                this.blocks.Add(type, d);
                return d;
            }
            private Dictionary<string, Block> GetDic(string type, string lang)
            {
                string k = lang == null ? DEFSTR : lang;
                Dictionary<string, Dictionary<string, Block>> d = GetDic(type);
                if (d.ContainsKey(k))
                    return d[k];
                Dictionary<string, Block> dd = new Dictionary<string, Block>();
                d.Add(k, dd);
                return dd;
            }
            public ICollection<string> GetBlockKeys(string type)
            {
                Dictionary<string, Dictionary<string, Block>> d = GetDic(type);
                return d.Keys;
            }
            public Block GetBlock(string type, string lang, string blockId)
            {
                Dictionary<string, Block> d = GetDic(type, lang);
                if (d.ContainsKey(blockId))
                    return d[blockId];
                string bN = RemoveLangPrefix(blockId);
                if(d.ContainsKey(bN))
                    return d[bN];
                if (blockId.Contains("."))
                {
                    foreach (string s in d.Keys)
                        if (s.IndexOf(blockId) > -1) return d[s];
                }
                return null;
            }
            public string GetText(string type, string lang, string blockId, string uBase)
            {
                Block b = this.GetBlock(type, lang, blockId);
                if (b != null && b.KeyExists(uBase)) return b.GetText(uBase);
                return null;
            }
            public bool HasTextKey(string type, string lang, string blockId, string uBase)
            {
                Block b = this.GetBlock(type, lang, blockId);
                return b != null && b.KeyExists(uBase);
            }
            public bool HasTextValue(string type, string lang, string blockId, string valStr)
            {
                Block b = this.GetBlock(type, lang, blockId);
                return b != null && b.ValueExists(valStr);
            }
            public ICollection<string> Types
            {
                get { return this.blocks.Keys; }
            }
            public ICollection<string> GetKeys(string type)
            {
                if (this.blocks.ContainsKey(type))
                    return this.blocks[type].Keys;
                return null;
            }
            
            public ICollection<string> GetKeys(string type, string lang)
            {
                return GetDic(type, lang).Keys;
            }
            public ICollection<string> GetKeys(string type, string lang, string blockId)
            {
                return GetBlock(type, lang, blockId).Keys;
            }
            public string Name
            {
                get { return name; }
            }
            public string Path
            {
                get { return path; }
            }
            public string Info()
            {
                StringBuilder s = new StringBuilder();
                foreach (string t in Types)
                {
                    s.Append(t);
                    Dictionary<string, Dictionary<string, Block>> d = GetDic(t);
                    s.Append('[');
                    foreach (string l in d.Keys)
                    {
                        s.Append(l);
                        Dictionary<string, Block> b = d[l];
                        int c = 0;
                        foreach (string x in b.Keys)
                            c = c + b[x].Count;
                        s.Append(':').Append(b.Count).Append(':').Append(c).Append(' ');
                    }
                    s.Append("], ");
                }

                return s.ToString(0, s.Length - 2);
            }

        }
        //-------------------------------------
        internal class Block
        {
            private string type;
            private string name;
            private string lang;
            private string size;
            private Dictionary<string, string> texts;
            public Block(string type, string name, string lang, string size)
            {
                this.type = type;
                this.name = name;
                this.lang = lang;
                this.size = size;
                this.texts = new Dictionary<string, string>();
            }
            public string Type
            {
                get { return type; }
            }
            public string Name
            {
                get { return name; }
            }
            public string Lang
            {
                get { return lang; }
            }
            public string Size
            {
                get { return size; }
            }

            public void AddText(string key, string value)
            {
                this.texts.Add(key, value);
            }
            public string GetText(string key)
            {

                return KeyExists(key) ? this.texts[key] : null;
            }
            public bool KeyExists(string key)
            {
                return this.texts.ContainsKey(key);
            }
            public ICollection<string> Keys
            {
                get { return this.texts.Keys; }
            }
            public void RemoveText(string key)
            {
                this.texts.Remove(key);
            }
            public bool ValueExists(string val)
            {
                return this.texts.ContainsValue(val);
            }
            public string GetKey(string val)
            {
                foreach (string k in this.texts.Keys)
                {
                    if (this.texts[k].Equals(val)) return k;
                }
                return null;
            }
            public int Count
            {
                get { return this.texts.Count; }
            }
        }
        //-------------------------------------
        internal class Opt
        {
            private Dictionary<string, string[]> dicParams;
            private Dictionary<string, string> altLangCodes;
            private List<string> groupNames;
            private Dictionary<string, string> fileMaps;
            private List<string> nonCmpKeys;
            private string strOutDir = "ResCmpResult";
            private string strOutXml = "ResCmpResult.xml";
            private bool bVerbose = false;
            private bool bRecurse = false;
            private bool bSattStruc = false;
            private bool bVerify = false;
            private string refKey = null;
            private string[] refPaths = null;
            private string strComment = null;
            public const string DEFKEY = "$DEFKEY$";
            private string resXslFile = null;
            private string mrgXslFile = null;
            private bool bSupDupValues = false;
            private bool bSupEmpValues = false;
            private bool bSupGrpItems = false;
            private Mode mMode = Mode.UNKNOWN;

            public Opt(string[] args)
            {
                dicParams = new Dictionary<string, string[]>();
                altLangCodes = new Dictionary<string, string>();
                fileMaps = new Dictionary<string, string>();
                //altLangCodes.Add("es", "1034");
                groupNames = new List<string>();
                nonCmpKeys = new List<string>();
                groupNames.Add("items");

                string v = string.Empty;
                for (int i = 0; i < args.Length; i++)
                {
                    string s = args[i];

                    if (s.StartsWith("/"))
                    {
                        v = s.ToLower();
                        this.bRecurse |= v.Equals("/r"); //recursive
                        this.bVerbose |= v.Equals("/v"); //verbose
                        this.bSattStruc |= v.Equals("/s"); //satellite structure
                        this.bVerify |= v.Equals("/w"); //do single file verificaiton
                        this.bSupDupValues |= v.Equals("/sdv");
                        this.bSupEmpValues |= v.Equals("/sev");
                        this.bSupGrpItems |= v.Equals("/sgi");
                    }
                    else
                    {
                        switch (v)
                        {
                            case "/d": //diff
                                if (s.Contains("="))
                                {
                                    mMode = Mode.DIFF;
                                    AddParam(s);
                                }
                                else
                                {
                                    Console.WriteLine("{0} option should have a parameter.", v);
                                    Console.WriteLine(" /d\t old=[file1;...] or old=[dir1;...]");
                                }
                                break;
                            case "/c": //compare                               
                                if (s.Contains("="))
                                {
                                    mMode = Mode.COMP;
                                    AddParam(s);
                                }
                                else
                                {
                                    Console.WriteLine("{0} option should have a parameter.", v);
                                    Console.WriteLine(" /c\t [lang]=[file1;...]");
                                }
                                break;
                            case "/z": //compare                               
                                if (s.Contains("="))
                                {
                                    mMode = Mode.AUTOCOMP;
                                    AddParam(s);
                                }
                                else
                                {
                                    Console.WriteLine("{0} option should have a parameter.", v);
                                    Console.WriteLine(" /z\t [lang]=[file1;...]");
                                }
                                break;
                            case "/j": //compare                               
                                if (s.Contains("="))
                                {
                                    mMode = Mode.AUTODIFF;
                                    AddParam(s);
                                }
                                else
                                {
                                    Console.WriteLine("{0} option should have a parameter.", v);
                                    Console.WriteLine(" /d\t old=[oldPath1;...] new=[newPath1;...]");
                                }
                                break;
                            case "/m": //map                               
                                if (s.Contains("="))
                                {
                                    string[] m = s.Split('=');
                                    string m0 = m[0].EndsWith(".xml",StringComparison.CurrentCultureIgnoreCase) ?
                                        m[0].ToLower() : m[0].ToLower() + ".xml";
                                    string m1 = m[1].EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase) ?
                                        m[1].ToLower() : m[1].ToLower() + ".xml";
                                    if(!this.fileMaps.ContainsKey(m0))
                                        this.fileMaps.Add(m0,m1);
                                    if (!this.fileMaps.ContainsKey(m1))
                                        this.fileMaps.Add(m1, m0);
                                }
                                else
                                {
                                    Console.WriteLine("{0} option should have a parameter.", v);
                                    Console.WriteLine(" /m\t file1=file2");
                                }
                                break;
                            case "/a": //alternate lang codes
                                if (s.Contains("="))
                                {
                                    string[] m = s.Split('=');
                                    if (!altLangCodes.ContainsKey(m[0]))
                                        altLangCodes.Add(m[0], m[1]);
                                }
                                else
                                {
                                    Console.WriteLine("{0} option should have a parameter.", v);
                                    Console.WriteLine(" /a\t lang=altLangCode e.g. es=1034");
                                }
                                break;

                            case "/g": //group items of suffix
                                    this.groupNames.AddRange(s.Split(';'));                                
                                break;
                            case "/i": //group items of suffix
                                this.nonCmpKeys.AddRange(s.ToLower().Split(';'));
                                break;
                            case "/t": //result xml stylesheet
                                this.resXslFile = s;
                                break;

                            case "/tm": //merged xml stylesheet
                                this.mrgXslFile = s;
                                break;

                            case "/x": //comment
                                this.strComment = s;
                                break;
                            case "/o": //outpur dir
                                this.strOutDir = s;
                                break;
                            case "/n": //output xml file name
                                this.strOutXml = s.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase) ? s : s + ".xml";
                                break;

                            case "/r": //recursive
                            case "/v": //verbose
                            case "/s": //satellite struc
                                Console.WriteLine("{0} should not have a parameter.", v);
                                break;
                            default:
                                if (v.Equals(string.Empty)) Console.WriteLine("No option specified for this parameter.");
                                else Console.WriteLine("Unsupported option: " + v);
                                break;
                        }
                    }
                }
            }
            public  void CopyParams(Dictionary<string, string[]> d)
            {
                foreach (string k in dicParams.Keys)
                    d.Add(k, dicParams[k]);      
            }
            
            private void AddParam(string s)
            {
                string[] m = s.Split('=');
                if (this.refKey == null)
                {
                    this.refKey = m[0];
                    this.refPaths = m[1].Split(';');
                }
                else
                {
                    dicParams.Add(m[0], m[1].Split(';'));
                }
            }
            public void ChangeParam(string key, string[] val)
            {
                if(dicParams.ContainsKey(key)) dicParams.Remove(key);
                dicParams.Add(key, val);
            }
            public int ParamCount
            {
                get { return this.dicParams.Count ; }
            }
            public bool IsParamAFile
            {
                get { return this.ReferencePaths.Length == 1 && File.Exists(this.ReferencePaths[0]); }
            }
            public ICollection<string> FileMapKeys
            {
                get { return this.fileMaps.Keys; }
            }
            public string GetMappedFile(string fileName)
            {
                string f = fileName.ToLower();
                return HasFileMap(f) ? this.fileMaps[f] : null;
            }

            public bool HasFileMap(string refFileName)
            {
                return this.fileMaps.ContainsKey(refFileName.ToLower());
            }
            public bool IsInGroupedNames(string suff)
            {
                if(this.groupNames.Contains(suff)) return true;
                foreach(string s in this.groupNames)
                    if(suff.StartsWith(s)) return true;
                return false;
            }
            public bool IsInNoCompareKeys(string keyName)
            {
                return this.nonCmpKeys.Contains(keyName.ToLower());
            }
            public bool HasAltCode(string langStr)
            {
                return altLangCodes.ContainsKey(langStr);
            }
            public string GetAltCode(string langStr)
            {
                return altLangCodes[langStr];
            }
            public string[] Param(string key)
            {
                return dicParams.ContainsKey(key) ? dicParams[key] : null;
            }
            public ICollection<string> Keys
            {
                get { return this.dicParams.Keys; }
            }
            public ICollection<string[]> Params
            {
                get { return this.dicParams.Values ; }
            }
            public bool HasParam(string verb)
            {
                return dicParams.ContainsKey(verb);
            }
            public bool IsSuppressDupValues
            {
                get { return this.bSupDupValues; }
            }
            public bool IsSuppressEmpValues
            {
                get { return this.bSupEmpValues; }
            }
            public bool IsSuppressGrpItems
            {
                get { return this.bSupGrpItems; }
            }
            public string ReferenceKey
            {
                get { return this.refKey; }
                set { this.refKey = value; }
            }
            public string[] ReferencePaths
            {
                get { return this.refPaths; }
                set { this.refPaths = value; }
            }
            public string Comment
            {
                get { return this.strComment; }
                set { this.strComment = value; }
            }
            public string OutDir
            {
                get { return strOutDir; }
                set { this.strOutDir = value; }
            }
            
            public string ResultXSLFile
            {
                get { return this.resXslFile; }
            }
            public string MergedXSLFile
            {
                get { return this.mrgXslFile; }
            }
            public string OutXmlName
            {
                get { return strOutXml; }
                set { this.strOutXml = value; }
            }
            public string OutXmlPath
            {
                get { return this.strOutDir + "\\" + strOutXml; }
            }
            public bool IsVerbose
            {
                get { return bVerbose; }
            }
            public bool IsRecursive
            {
                get { return bRecurse; }
            }
            public bool IsSRStructure
            {
                get { return bSattStruc; }
                set { bSattStruc = value; }
            }
            public Mode Mode
            {
                get { return mMode; }
                set { this.mMode = value; }
            }
            public string MergedXmlDir
            {
                get { return this.strOutDir + "\\mergedXML"; }
            }
            
        }
        //-------------------------------------
        internal class MergedModule
        {
            private string moduleName;
            //key(lang), filepath
            private Dictionary<string, string> files;
            //type, blockId
            private Dictionary<string, Dictionary<string, MergedBlock>> blocks;
            public MergedModule(string modName)
            {
                this.moduleName = modName;
                this.blocks = new Dictionary<string, Dictionary<string, MergedBlock>>();
                this.files = new Dictionary<string, string>();
            }
            public int BlockCount
            {
                get { return this.blocks.Count; }
            }
            public int FileCount
            {
                get { return this.files.Count; }
            }
            public void AddFile(string key, string filePath)
            {
                this.files.Add(key, filePath);
            }
            private Dictionary<string, MergedBlock> GetDic(string type)
            {
                if (!this.blocks.ContainsKey(type)) this.blocks.Add(type, new Dictionary<string, MergedBlock>());
                return this.blocks[type];
            }
            public void AddBlock(string type, string blockId, MergedBlock mrgBlock)
            {
                GetDic(type).Add(blockId, mrgBlock);
            }
            public ICollection<string> FileKeys
            {
                get { return this.files.Keys; }
            }
            public ICollection<string> BlockIds
            {
                get { return this.blocks.Keys; }
            }
            public MergedBlock GetMergedBlock(string type, string blockId)
            {
                return GetDic(type)[blockId];
            }
            public string GetFilePath(string key)
            {
                return this.files[key];
            }
        }
        //-------------------------------------
        internal class MergedBlock
        {
            private string resourceType;
            private Dictionary<string, MergedValues> values;
            public MergedBlock(string resType)
            {
                this.resourceType = resType;
                this.values = new Dictionary<string, MergedValues>();
            }
            public int ValueCount
            {
                get { return this.values.Count; }
            }
            public void AddValue(string key, MergedValues mrgVal)
            {
                this.values.Add(key, mrgVal);
            }
            public ICollection<string> Keys
            {
                get { return this.values.Keys; }
            }
            public string ResourceType
            {
                get { return this.resourceType; }
            }
        }
        //-------------------------------------
        internal class MergedValues
        {
            private string refKey;
            private string refText;
            private Dictionary<string, string> texts;
            public MergedValues(string refKey, string refText)
            {
                this.refKey = refKey;
                this.refText = refText;
                this.texts = new Dictionary<string, string>();
            }
            public void AddText(string key, string text)
            {
                this.texts.Add(key, text);
            }
            public int Count
            {
                get { return this.texts.Count; }
            }
            public string ReferenceKey
            {
                get { return this.refKey; }
            }
            public string ReferenceText
            {
                get { return this.refText; }
            }
        }
        //-------------------------------------
        internal struct ResourceEntry
        {
            public string moduleName;
            public string language;
            public string blockName;
            public string resourceType;
            public string resourceKey;
            public string resourceValue;
            public string otherData;
            public ResourceEntry(string modName, string resTyp, string lang, string bkId, string resKey, string resVal, string othDat)
            {
                this.moduleName = modName;
                this.resourceType = resTyp;
                this.language = lang;
                this.blockName = bkId;
                this.resourceKey = resKey;
                this.resourceValue = resVal;
                this.otherData = othDat;
               
            }
            public ResourceEntry(string modName, string resTyp, string lang, string bkId, string resKey, string resVal):this(modName, resTyp, lang,bkId,resKey,resVal,null)
            {
            }
        }
        internal struct DuplicateEntry
        {
            public ResourceEntry entryOne;
            public ResourceEntry entryTwo;
            public DuplicateEntry(ResourceEntry r1, ResourceEntry r2)
            {
                this.entryOne = r1;
                this.entryTwo = r2;
            }
        }
        //-------------------------------------
        internal class ValueAuditor
        {
            private Opt opt;
            private List<DuplicateEntry> duplicateKeys;
            private List<DuplicateEntry> duplicateValues;

            public ValueAuditor(Opt opt)
            {
                this.opt = opt;

            }
            public void AddDuplicateKey(ResourceEntry r1, ResourceEntry r2)
            {
                if (this.duplicateKeys == null) this.duplicateKeys = new List<DuplicateEntry>();
                this.duplicateKeys.Add(new DuplicateEntry(r1, r2));
            }
            public void AddDuplicateValue(ResourceEntry r1, ResourceEntry r2)
            {
                if (this.duplicateValues == null) this.duplicateValues = new List<DuplicateEntry>();
                this.duplicateValues.Add(new DuplicateEntry(r1, r2));
            }

        }
        internal class GroupedRes
        {
            private string name;
            private string prefix;
            private string lang;
            private Dictionary<string, string> kvPairs;
            private Dictionary<string, GroupedRes> subGroups;
            public GroupedRes(string name, string pref, string lang)
            {
                this.name = name;
                this.prefix = pref;
                this.lang = lang;
                this.subGroups = new Dictionary<string, GroupedRes>();
                this.kvPairs = new Dictionary<string, string>();
            }
            public int KVCount
            {
                get { return kvPairs.Count; }
            }
            public void Remove(string key)
            {
                if (kvPairs.ContainsKey(key)) kvPairs.Remove(key);
                foreach (string gk in new List<string>(subGroups.Keys))
                {
                    GroupedRes g = subGroups[gk];
                    g.Remove(key);
                    if (g.KVCount == 0)
                        this.subGroups.Remove(gk);
                }
            }
            public void AddKeyValue(string key, string value)
            {
                if (!this.kvPairs.ContainsKey(key))
                    this.kvPairs.Add(key, value);
            }
            public string Lang
            {
                get { return this.lang; }
            }
            public string Name
            {
                get { return this.name; }
            }
            public string Prefix
            {
                get { return this.prefix; }
            }
            public ICollection<string> Keys
            {
                get { return this.kvPairs.Keys; }
            }
            public string GetValue(string key)
            {
                return this.kvPairs[key];
            }
            public int Count
            {
                get { return this.kvPairs.Count; }
            }
            public void AddSubGroup(GroupedRes gr)
            {
                if (gr != null && !this.subGroups.ContainsKey(gr.Lang))
                    this.subGroups.Add(gr.Lang, gr);
            }
            public bool HasSubGroups()
            {
                return this.subGroups.Count > 0;
            }
            public GroupedRes GetSubGroup(string lang)
            {
                return this.subGroups[lang];
            }
            public ICollection<string> GroupKeys
            {
                get { return this.subGroups.Keys; }
            }
        }
        internal class ResGroupMgr
        {
            private Opt opt;
                                //mod             //lang            //block           //key 
            private Dictionary<string,Dictionary<string, Dictionary<string,Dictionary<string, GroupedRes>>>> grpRes;
            public ResGroupMgr(Opt o)
            {
                this.opt = o;
                this.grpRes = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, GroupedRes>>>>();
            }

            public GroupedRes Add(string modName, string lang, string blkName, string key, string val)
            {

                int lip = key.LastIndexOf('.');

                if (lip < 1) return null;
                string kName = key.Substring(0, lip);
                string pName = key.Substring(lip + 1).ToLower();

                if(!this.opt.IsInGroupedNames(pName)) return null;

                Dictionary<string, GroupedRes> d = GetDic(modName, lang, blkName);


                GroupedRes g;
                if (d.ContainsKey(kName))
                {
                    g = d[kName];
                }
                else
                {
                    g = new GroupedRes(kName, pName, lang);
                    d.Add(kName, g);
                }
                g.AddKeyValue(key, val);

               /* if (!this.opt.ReferenceKey.Equals(lang))
                {
                    GroupedRes eGrp = this.Get(modName, this.opt.ReferenceKey, blkName, key);
                    if (eGrp != null)
                    {
                        eGrp.AddSubGroup(g);
                    }
                }
                * */
                return g;
           } 
            private Dictionary<string, Dictionary<string, Dictionary<string, GroupedRes>>> GetDic(string modName)
            {
                if (!this.grpRes.ContainsKey(modName))
                {
                    this.grpRes.Add(modName, new Dictionary<string, Dictionary<string, Dictionary<string, GroupedRes>>>());
                }
                return this.grpRes[modName];
            }
            private Dictionary<string, Dictionary<string, GroupedRes>> GetDic(string modName, string lang)
            {
                Dictionary<string, Dictionary<string, Dictionary<string, GroupedRes>>> d = GetDic(modName);
                if (!d.ContainsKey(lang))
                    d.Add(lang, new Dictionary<string, Dictionary<string, GroupedRes>>());
                return d[lang];
            }
            private Dictionary<string, GroupedRes> GetDic(string modName, string lang, string bkName)
            {
                Dictionary<string, Dictionary<string, GroupedRes>> d = GetDic(modName, lang);
                if (!d.ContainsKey(bkName))
                    d.Add(bkName, new Dictionary<string, GroupedRes>());
                return d[bkName];
            }
            public ICollection<string> Keys(string modName, string lang, string blk)
            {
                return GetDic(modName, lang, blk).Keys; 
            }
            public ICollection<string> Keys(string modName, string lang)
            {
                return GetDic(modName, lang).Keys;
            }
            public ICollection<string> Keys(string modName)
            {
                return GetDic(modName).Keys;
            }
            public GroupedRes Get(string modName, string lang, string blk, string key)
            {
                Dictionary<string, GroupedRes> d = GetDic(modName, lang, blk);
                if (d.ContainsKey(key)) return d[key];
                return null;
            }
            public void WriteToXML2(XmlWriter w)
            {
                w.WriteStartElement("groupedItems");
                foreach (string m in this.grpRes.Keys)
                {
                    foreach (string l in Keys(m))
                    {
                        foreach(string b in Keys(m,l))
                        {
                            foreach(string k in Keys(m,l,b))
                            {
                                GroupedRes g = Get(m, l, b, k);
                                    w.WriteStartElement("module");
                                    w.WriteAttributeString("name", m);
                                    w.WriteStartElement("string");
                                    w.WriteAttributeString("block", b);
                                    w.WriteAttributeString("lang", l);


                                    foreach (string x in g.Keys)
                                    {
                                        w.WriteStartElement("text");
                                        w.WriteAttributeString("key", x);
                                        w.WriteString(g.GetValue(x));
                                        w.WriteEndElement();//s
                                    }
                                    w.WriteEndElement();//block
                                    w.WriteEndElement(); //module 
                            }
                        }
                    }
                }
                w.WriteEndElement(); //groupedItems  


            }
            public void WriteToXML3(XmlWriter w)
            {
                w.WriteStartElement("groupedItems");
                foreach (string m in this.grpRes.Keys)
                {

                        foreach (string b in Keys(m, this.opt.ReferenceKey))
                        {
                            foreach (string k in Keys(m, this.opt.ReferenceKey, b))
                            {
                                GroupedRes g = Get(m, this.opt.ReferenceKey, b, k);
                               // if (g.Count > 1)
                               // {
                                    w.WriteStartElement("module");
                                    w.WriteAttributeString("name", m);
                                    w.WriteAttributeString("block", b);

                                    w.WriteStartElement("string");
                                    w.WriteAttributeString("lang", this.opt.ReferenceKey);

                                    foreach (string x in g.Keys)
                                    {
                                        w.WriteStartElement("text");
                                        w.WriteAttributeString("key", x);
                                        w.WriteString(g.GetValue(x));
                                        w.WriteEndElement();//s
                                    }
                                    w.WriteEndElement();//string

                                    if (g.HasSubGroups())
                                    {
                                        foreach (string xx in g.GroupKeys)
                                        {
                                            GroupedRes sg = g.GetSubGroup(xx);
                                            w.WriteStartElement("string");
                                            w.WriteAttributeString("lang", xx);

                                            foreach (string y in sg.Keys)
                                            {
                                                w.WriteStartElement("text");
                                                w.WriteAttributeString("key", y);
                                                w.WriteString(sg.GetValue(y));
                                                w.WriteEndElement();//
                                            }
                                            w.WriteEndElement();//string
                                        }
                                    }

                                    w.WriteEndElement(); //module 
                                //}
                            }
                        }
                }
                w.WriteEndElement(); //groupedItems  


            }
            public void WriteToXML(XmlWriter w)
            {
                w.WriteStartElement("groupedItems");
                foreach (string m in this.grpRes.Keys)
                {

                    foreach (string b in Keys(m, this.opt.ReferenceKey))
                    {
                        foreach (string k in Keys(m, this.opt.ReferenceKey, b))
                        {
                            GroupedRes g = Get(m, this.opt.ReferenceKey, b, k);
                            w.WriteStartElement("module");
                            w.WriteAttributeString("name", m);
                            w.WriteAttributeString("block", b);

                            w.WriteStartElement("control");
                            w.WriteAttributeString("name", k);


                            //per language view
                            w.WriteStartElement("string");
                            w.WriteAttributeString("view", "perLang");

                            w.WriteStartElement("group");
                            w.WriteAttributeString("lang", this.opt.ReferenceKey);
                            w.WriteAttributeString("count", g.KVCount.ToString());

                            foreach (string x in g.Keys)
                            {
                                w.WriteStartElement("text");
                                w.WriteAttributeString("key", x);
                                w.WriteString(g.GetValue(x));
                                w.WriteEndElement();//s
                            }
                            w.WriteEndElement();//group

                            if (g.HasSubGroups())
                            {
                                foreach (string xx in g.GroupKeys)
                                {
                                    GroupedRes sg = g.GetSubGroup(xx);
                                    w.WriteStartElement("group");
                                    w.WriteAttributeString("lang", xx);
                                    w.WriteAttributeString("count", sg.KVCount.ToString());
                                    if(g.KVCount > sg.KVCount)
                                        w.WriteAttributeString("missingItems",(g.KVCount - sg.KVCount) +"" );
                                    if (g.KVCount < sg.KVCount)
                                        w.WriteAttributeString("extraItems", (sg.KVCount - g.KVCount) + "");

                                    foreach (string y in sg.Keys)
                                    {
                                        w.WriteStartElement("text");
                                        w.WriteAttributeString("key", y);
                                        w.WriteString(sg.GetValue(y));
                                        w.WriteEndElement();//
                                    }
                                    w.WriteEndElement();//group
                                }
                            }
                            w.WriteEndElement();//string

                            //per key view
                            w.WriteStartElement("string");
                            w.WriteAttributeString("view", "perKey");
                            foreach (string x in new List<string>(g.Keys))
                            {
                                w.WriteStartElement("group");
                                w.WriteAttributeString("key", x);
                                w.WriteStartElement("text");
                                w.WriteAttributeString("lang", this.opt.ReferenceKey);
                                w.WriteString(g.GetValue(x));
                                w.WriteEndElement();//s
                                //subgroups
                                foreach (string xx in g.GroupKeys)
                                {
                                    GroupedRes sg = g.GetSubGroup(xx);
                                    w.WriteStartElement("text");
                                    w.WriteAttributeString("lang", xx);
                                    w.WriteString(sg.GetValue(x));
                                    w.WriteEndElement();//
                                }
                                w.WriteEndElement();//
                                g.Remove(x);
                            }
                            w.WriteEndElement();//string
                            
                            //new entries in EN
                            if (g.KVCount > 0)
                            {
                                w.WriteStartElement("newEntries");
                                w.WriteAttributeString("lang", this.opt.ReferenceKey);
                                foreach (string x in g.Keys)
                                {
                                    w.WriteStartElement("text");
                                    w.WriteAttributeString("key", x);
                                    w.WriteString(g.GetValue(x));
                                    w.WriteEndElement();
                                }
                                w.WriteEndElement();//newEntries
                            }
                            //new entries in others
                            if (g.HasSubGroups())
                            {
                               //subgroups
                                foreach (string xx in g.GroupKeys)
                                {
                                    GroupedRes sg = g.GetSubGroup(xx);
                                    if (sg.KVCount > 0)
                                    {
                                        w.WriteStartElement("extraEntries");
                                        w.WriteAttributeString("lang", xx);
                                        foreach (string x in sg.Keys)
                                        {
                                            w.WriteStartElement("text");
                                            w.WriteAttributeString("key", x);
                                            w.WriteString(sg.GetValue(x));
                                            w.WriteEndElement();
                                        }
                                        w.WriteEndElement();//newEntries
                                    }
                                }
                            }
                            w.WriteEndElement();//control
                            w.WriteEndElement(); //module 
                            //}
                        }
                    }
                }
                w.WriteEndElement(); //groupedItems  


            }

        }
        public enum Mode { DIFF, COMP, AUTOCOMP, AUTODIFF, UNKNOWN };


    }
}

