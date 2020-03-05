@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off 

IF "%DEPLOYMENT_SCRIPT%" neq "" (
  echo Running the deployment script %DEPLOYMENT_SCRIPT%
  call %DEPLOYMENT_SCRIPT%
) ELSE (
  echo DEPLOYMENT_SCRIPT setting is required
  exit /b 1
)