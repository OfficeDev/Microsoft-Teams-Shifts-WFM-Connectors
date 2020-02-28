@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off

IF "%SITE_ROLE%" == "api" (
  deploy.api.cmd
) ELSE (
  IF "%SITE_ROLE%" == "configuration" (
    deploy.configuration.cmd
  ) ELSE (
    echo You have to set SITE_ROLE setting to either "api" or "configuration"
    exit /b 1
  )
)