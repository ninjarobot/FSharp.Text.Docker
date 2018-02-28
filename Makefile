.PHONY: build

OS := $(shell uname)
MONO_PATH := $(shell which mono 2>/dev/null)
ifeq (,$(MONO_PATH))
$(error mono is required for building net45 applications, but is not installed)
endif

Configuration?=Release

ifeq ($(OS),Darwin)
	MONO_LIB := $(MONO_PATH)/../../lib/mono/4.5/
else ifeq ($(OS),Linux)
	MONO_LIB := $(MONO_PATH)/../../lib/mono/4.5/
endif

build:
	dotnet build /p:FrameworkPathOverride=$(MONO_LIB)

all: test pack

test:
	dotnet test /p:FrameworkPathOverride=$(MONO_LIB) tests/FSharp.Text.Docker.Tests.fsproj

pack:
	dotnet pack /p:FrameworkPathOverride=$(MONO_LIB) -c $(Configuration)
