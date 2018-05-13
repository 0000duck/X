param($installPath, $toolsPath, $package, $project)


try
{
    # ���а���ŵ�ַ
    $allPakPath = Split-Path -Parent $installPath
    $allPakPath = $allPakPath + "/"

    # ���ļ�����
    $corePName = "NewLife.Core"
    $xcodePName = "NewLife.XCode"    

    #��ȡ�汾��
    
        # nuget������
        $packageCfg = $project.ProjectItems.Item("packages.config")

        # nuget�������ļ��� 
        $packageCfgPath = $packageCfg.Properties("FullPath").Value

        # ��ȡ�ڵ�
        $xmlDoc = New-Object "System.Xml.XmlDocument"  
        $xmlDoc.Load($packageCfgPath)
        $coreNode = $xmlDoc.SelectSingleNode("/packages/package[@id='NewLife.Core']")
        $xcodeNode = $xmlDoc.SelectSingleNode("/packages/package[@id='NewLife.XCode']")

        # �汾��
        $coreV = $coreNode.version
        $xcodeV = $xcodeNode.version
    
    
    # ����·��
    $pPath = "lib/net40/";

      # �ļ���
    $coreDllName = "NewLife.Core.dll"
    $xcodeDllName = "XCode.dll" 

    # Դ��ַ
    $coreSrc = $allPakPath + $corePName + "." + $coreV + "/" + $pPath + $coreDllName
    $xcodeSrc = $allPakPath + $xcodePName + "." + $xcodeV + "/" + $pPath + $xcodeDllName

    # Ŀ���ļ���
    $tarDir = "DLL"
    if(!( Test-Path $tarDir ))
    {
        mkdir $tarDir
    }

    #Ŀ���ַ
    $coreTar = $tarDir + "/" + $coreDllName
    $xcodeTar = $tarDir + "/" + $xcodeDllName

    #�����ļ�
    Copy-Item $coreSrc $coreTar
    Copy-Item $xcodeSrc $xcodeTar
}
catch
{
    "����dll�ļ��������ֶ�����xcode.dll��newlife.core.dll����ĿĿ¼DLL�ļ���" | Out-File debug.txt
}