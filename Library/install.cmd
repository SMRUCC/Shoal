@echo off

CD "../App/"

REM Install a given list of local system library modules into 
REM the repository database of R# package system.
R# install.packages('R.base.dll');
R# install.packages('R.code.dll');
R# install.packages('R.graph.dll');
R# install.packages('R.graphics.dll');
R# install.packages('R.plot.dll');
R# install.packages('R.math.dll');

R# install.packages('devkit.dll');
R# install.packages('signalKit.dll');
R# install.packages('Rstudio.dll');
R# install.packages('MLkit.dll');
R# install.packages('webKit.dll');

REM finally, view of the summary information about the installed
REM libraries.
R# installed.packages();

pause
