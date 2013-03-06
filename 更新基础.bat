:: ����Ŀ��Ҫ����һЩ�����ļ���������
:: 1�����������������Ŀ¼�ļ�web.config,favicon.ico,Global.asax,index.htm
:: 2���Աȸ���Web\App_Code,Web\Admin,Css��Scripts
:: 3�������ļ�DLL

@echo off
cls
setlocal enabledelayedexpansion
title ���»����ļ�

:: ������Դ��ַ
:: Ϊ������ٶȣ����Բ��ñ��ص�ַ
if not exist C:\X (
	set svn=https://svn.nnhy.org/svn/X/trunk
) else (set svn=C:\X)
set url=%svn%/trunk

:: 1�������������
if not exist Web md Web
if not exist WebData md WebData

:: ���浱ǰĿ¼�����л�Ŀ¼
pushd Web
set url=%svn%/Src/Web
:: do else �ȹؼ���ǰ��Ӧ��Ԥ���ո�
for %%i in (Web.config Default.aspx Default.aspx.cs favicon.ico Global.asax index.htm) do (
	if not exist %%i svn export --force %url%/%%i %%i
)

:: 2���Աȸ���Web\App_Code,Web\Admin,Css��Scripts
set url=%svn%/Src/Web
for %%i in (App_Code Admin Css Scripts) do (
	if exist %%i (
		pushd %%i
		for /r %%f in (*.*) do (
			set name=%%f
			set name=!name:%cd%\%%i\=!
			::echo !name!
			svn export --force %url%/%%i/!name:\=/! !name!
		)
		popd
	) else (
		svn export --force %url%/%%i %%i
	)
)
:: �ָ�Ŀ¼
popd

:: 3�������ļ�DLL
set name=DLL
set url=%svn%/%name%
if exist %name% (
	pushd %name%
	for /r %%f in (*.*) do svn export --force %url%/%%~nxf %%~nxf
	popd
) else (
	svn export --force %url% %name%
)

pause