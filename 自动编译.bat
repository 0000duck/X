:: �Զ�����X����������µ�DLL��ȥ
:: 1����������Դ��Src
:: 2������DLL
:: 3�������������
:: 4������DLL
:: 5���ύDLL����
:: 6�����Src��DLL��FTP

::@echo off
::cls
setlocal enabledelayedexpansion
title �Զ�����

:: 1����������Դ��Src
:: 2������DLL
:: ���浱ǰĿ¼�����л�Ŀ¼
pushd ..
set svn=https://svn.nnhy.org/svn/X/trunk
:: do else �ȹؼ���ǰ��Ӧ��Ԥ���ո�
for %%i in (Src DLL XCoder) do (
	svn info %svn%/%%i
	svn update %%i
)
:: �ָ�Ŀ¼
popd

:: 3�������������
::"D:\MS\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" X���.sln /Build Release
set vs="D:\MS\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com"
for %%i in (NewLife.Core XCode NewLife.CommonEntity NewLife.Mvc NewLife.Net XAgent XControl XTemplate XCoder) do (
	%vs% X���.sln /Build Release /Project %%i
)

:: 4������DLL
copy ..\����\N*.* ..\DLL\ /y
copy ..\����\X*.* ..\DLL\ /y
del ..\DLL\*.config /f/s/q

for %%i in (XCoder.exe XCoder.exe.config NewLife.Core.dll XCode.dll XTemplate.dll) do (
	copy ..\��������\%%i ..\XCoder\%%i /y
)

:: 5���ύDLL����
svn commit -m "�Զ�����" ..\DLL
svn commit -m "�Զ�����" ..\XCoder

:: 6�����Src��DLL��FTP
set rar="C:\Program Files\WinRAR\RAR.exe" -m5 -md4096 -mt2 -s -z..\Src\Readme.txt
set zipfile=%date:~0,4%%date:~5,2%%date:~8,2%%time:~0,2%%time:~3,2%%time:~6,2%.rar
set dest=E:\����\����������\X

:: ����SrcԴ��
rd XCoder\bin /s/q
rd XCoder\obj /s/q
set zipfile=Src.rar
%rar% -r a %zipfile% NewLife.Core\*.cs NewLife.CommonEntity\*.cs XControl\*.cs XAgent\*.cs XCode\Entity\*.cs XCode\DataAccessLayer\Common\*.cs XCoder\*.* XTemplate\Templating\Template.cs
move /y %zipfile% %dest%\%zipfile%

:: ����XCode����Դ��
rd YWS\bin /s/q
rd YWS\obj /s/q
rd Web\bin /s/q
rd Web\Log /s/q
rd Web\App_Data /s/q
md Web\Bin
Copy ..\DLL\XControl.* Web\Bin\ /y
set zipfile=XCodeSample.rar
%rar% -r a %zipfile% YWS\*.* Web\*.*
move /y %zipfile% %dest%\%zipfile%

:: ����DLLѹ����
:: ���浱ǰĿ¼�����л�Ŀ¼
pushd ..\DLL
::"C:\Program Files\WinRAR\WinRAR.exe" a DLL.rar *.dll *.exe *.pdb *.xml
set zipfile=DLL.rar
%rar% a %zipfile% *.dll *.exe *.pdb *.xml
move /y %zipfile% %dest%\%zipfile%
:: �ָ�Ŀ¼
popd

:: ��������������XCoder
:: ���浱ǰĿ¼�����л�Ŀ¼
pushd ..\XCoder
set zipfile=XCoder.rar
%rar% a %zipfile% *.dll *.exe *.config
move /y %zipfile% %dest%\%zipfile%
:: �ָ�Ŀ¼
popd