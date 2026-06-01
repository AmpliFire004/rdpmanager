§SHELL := /bin/sh
DOTNET ?= dotnet
PROJECT := RdpManager/RdpManager.csproj
RUNTIME ?= win-x64
SELF_CONTAINED ?= true
PUBLISH_DIR ?= publish
VERSION ?= 1.0
FILE_VERSION ?= $(VERSION)
ASSEMBLY_VERSION ?= $(VERSION)


.PHONY: all build run clean publish release

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

release: publish
	@echo "Creating and pushing version tag v$(VERSION)"
	git tag -a v$(VERSION) -m "Release version $(VERSION)"
	git push origin main
	git push origin v$(VERSION)

clean:
	$(DOTNET) clean $(PROJECT)
