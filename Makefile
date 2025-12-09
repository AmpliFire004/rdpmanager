§SHELL := /bin/sh
DOTNET ?= dotnet
PROJECT := RdpManager/RdpManager.csproj
RUNTIME ?= win-x64
SELF_CONTAINED ?= true
PUBLISH_DIR ?= publish
VERSION ?= 1.0
FILE_VERSION ?= $(VERSION).0
ASSEMBLY_VERSION ?= $(VERSION).0


.PHONY: all build run clean publish

all: build

build:
	$(DOTNET) build $(PROJECT)

run:
	$(DOTNET) run --project $(PROJECT)

publish:
	$(DOTNET) publish $(PROJECT) -c Release -r $(RUNTIME) \
		--self-contained $(SELF_CONTAINED) \
		-p:Version=$(VERSION) \
		-p:AssemblyVersion=$(ASSEMBLY_VERSION) \
		-p:FileVersion=$(FILE_VERSION) \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishTrimmed=false \
		-o $(PUBLISH_DIR)

clean:
	$(DOTNET) clean $(PROJECT)
