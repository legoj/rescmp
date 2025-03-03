**rescmp** takes the XML files generated by **resdmp** (or *enumres*) and does the comparison of the resource entries. it does comparisons of the resources between releases (i.e. *cross-release*) 
and between the supported languages (i.e. *cross-language*) and generated an XML output on possible issues found. refer to the included documents on ***UI resource static verification***.

### note:
if you are dumping modules located in a network share, there might be some cases
where the modules could not be loaded due to security settings of your PC where 
your are running the tool. I would recommend you to copy the binaries locally and dump them.

### [Usage]

- cross-release compare:

      rescmp.exe /d rtm=<pathToRTMTokenDumps> [v1=<pathToNewerVersion>] [v2=<pathToLatestBuild>] [additional switches]

- automatic cross-release compare:

      rescmp.exe /j rtm=<rtmpath1;rtmpath2;...> hrp1=<hrp1path1;hrp1path2;...> [hrp2=<hrp1path1;hrp1path2;...>]... [additional switches]

- cross-languages compare:

      rescmp.exe /c en=<pathToENTokenDumps> [de=<pathToDETokenDumps>] [fr=<pathToFRTokenDumps>] [es=<pathToESTokenDumps>] [ja=<pathToJATokenDumps>]  [langCode=<pathToTokenDumps>] [additional switches]

- automatic cross-languages compare:

      rescmp.exe /z en=<rootpath> [additional switches]

**Additional Switches:**

    /r                      -recursive scan 
    /s                      -satellite resources structure 
    /x <comment>            -user comments 
    /o <pathToOutputDir>    -output directory 
    /n <xmlFileName>        -output filename 
    /sdv                    -suppress duplicate values result
    /sev                    -suppress empty values result
    /sgi                    -suppress group items result
    /a <lang=code>          -alternative language code e.g. es=1034
    /g <suffixString>       -key suffix string e.g. "items"
    /m <file1=file2>        -filename mapping e.g. "CVhdMount.exe=CVhdMountUI.dll"
    /t <result xml stylesheet>      -path to XSL file e.g. ResCmp_html.xsl"
    /tm <merged xml stylesheet>     -path to XSL file e.g. MergedXML.xsl"

### [Examples]
- cross-release comparison:

      rescmp.exe /d rtm=D:\ZZTMP\ohio_rtm\xa.sys32 hrp1=D:\ZZTMP\ohio_hrp1\xa.sys32 /x "Ohio System32 Files: RTM vs HRP1" /o "D:\ZZTMP\OhioHRP1" /n "rtm_vs_hrp1b17.xml"

- cross-languages comparison:
  
      rescmp.exe /c en=D:\ZZTMP\parra_dumps\xa.mgmt /s /x "Test Comment" /o "D:\ZZTMP\Build6334" /n "parra.xamgmt.B6334TokenDiff.xml"

- cross-language comparison for specified 2 languages

      rescmp /c en=D:\TMP\DEMO_RES\ZZResDumps\hrp05\PSE450W2K3R05\25\EN ja=D:\TMP\DEMO_RES\ZZResDumps\hrp05\PSJ450W2K3R05\25\JA /x "HRP05 B25 EN-JA Compare" /o D:\TMP\DEMO_RES\ResCompare\HRP05B25EN-JAComp /n HRP05B25EN-JAComp.xml

- cross-language compare on satellite resource structured folder (EN files at root)

      rescmp /c en=D:\TMP\DEMO_RES\ZZResDumps\Parra\xa.mgmt /s /x "Parra XAMgmtConsole Cross-Language Check" /o D:\TMP\DEMO_RES\ResCompare\ParraXAMgmtComp /n ParraXAMgmtComp.xml

- cross-language compare on satellite resource structured folder (EN files in subfolder)

      rescmp /c en=D:\TMP\DEMO_RES\ZZResDumps\Parra\sys32.resource /s /x "Parra System32 Resources Cross-Language Check" 	/o D:\TMP\DEMO_RES\ResCompare\ParraSys32Comp 	/n ParraSys32Comp.xml

- cross-language compare on resources with alternative culture

      rescmp /c en=D:\TMP\DEMO_RES\ZZResDumps\Parra\sys32.resource /s /x "Parra System32 Resources Cross-Language Check"	/a en=2057 es=2058 es=1034

- cross-language compare with mapped files

      rescmp /c en=D:\TMP\DEMO_RES\ZZResDumps\PVS /s /o D:\TMP\DEMO_RES\ZZResCompare\PVS /m CVhdMount.exe=CVhdMountUI.dll [file2.exe=file2UI.dll]

- cross-language compare with grouped items e.g. combo list boxes values

      rescmp /c en=D:\TMP\DEMO_RES\ZZResDumps\Parra\sys32.resource /s /x "Parra System32 Resources Cross-Language Check" 		/g options;items

### change history:
* 08/02/2010: added /t switch to specify XSL file in result xml file
* 08/04/2010: added /tm switch to specify XSL file in mergex xml files
* 08/19/2010: added /z switch for autosearch of resource folder and comparison.
* 09/07/2010: bug fix - diff did not show new/missing entries.
* 02/03/2011: reimplementation of missing entries from block level to resource entry level.
* 05/10/2011: added the alt code implementation for base res
* 08/31/2011: bug fix - ignore case in language folder. previously en != EN... 
* 11/19/2012: added placeholder ('%') count check in crosslang compare. locstrings with diff count will be marked with phDif attribute in the mergedXML file.
* 02/15/2013: added additional options to suppress the following results

      /sdv - suppress duplicate values
	   /sev - suppress empty values
	   /sgi - suppress group items

