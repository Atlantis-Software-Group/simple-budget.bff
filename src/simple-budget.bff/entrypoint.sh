#!/bin/sh

cd src
cd simple-budget.bff
dotnet restore ./simple-budget.bff.csproj
dotnet build ./simple-budget.bff.csproj
dotnet watch ./bin/Debug/net8.0/simple-budget.bff.dll