ECHO off

SET /P APPVEYOR_BUILD_NUMBER=Please enter a build number (e.g. 134):
SET /P PACKAGE_VERSION=Please enter your package version (e.g. 1.0.5):
SET /P version_suffix=Please enter your package release suffix or leave empty (e.g. beta):

SET /P APPVEYOR_REPO_TAG=If you want to simulate a GitHub tag for a release (e.g. true):

if "%APPVEYOR_BUILD_NUMBER%" == "" (
  SET APPVEYOR_BUILD_NUMBER=100
)
if "%PACKAGE_VERSION%" == "" (
  SET PACKAGE_VERSION=0.2.0
)



if "%POSTFIX%" == "" (
SET mssemver=%PACKAGE_VERSION%-%APPVEYOR_BUILD_NUMBER%
)
else   "%POSTFIX%" == "" (
SET mssemver=%PACKAGE_VERSION%-%version_suffix%-%APPVEYOR_BUILD_NUMBER%
)

SET CONFIGURATION=Debug

build-appveyor.cmd

@IF %ERRORLEVEL% NEQ 0 GOTO err
@EXIT /B 0
:err
@PAUSE
@EXIT /B 1