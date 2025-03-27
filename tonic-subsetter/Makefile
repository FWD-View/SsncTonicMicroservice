configuration = Debug
docker_repository = 127.0.0.1
docker_tag = latest

.PHONY: all clean

all: push
build: build-tonic-foreignkeymaskconfigure build-tonic-primarykeyremapper build-tonic-resetschema build-tonic-subsetter build-tonic-tablecount build-tonic-cleanbatch build-tonic-subsetterwebservice build-tonic-reuserowsalterations clean-images
push: push-tonic-foreignkeymaskconfigure push-tonic-primarykeyremapper push-tonic-resetschema push-tonic-subsetter push-tonic-tablecount push-tonic-cleanbatch push-tonic-subsetterwebservice push-tonic-reuserowsalterations clean-images
clean: clean-tonic-foreignkeymaskconfigure clean-tonic-primarykeyremapper clean-tonic-resetschema clean-tonic-subsetter clean-tonic-tablecount clean-tonic-cleanbatch clean-tonic-subsetterwebservice clean-tonic-reuserowsalterations

build-tonic-foreignkeymaskconfigure:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.ForeignKeyMaskConfigure --build-arg DOTNET_CORE_IMAGE=runtime --tag ${docker_repository}tonic-foreignkeymaskconfigure:${docker_tag} .

build-tonic-primarykeyremapper:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.PrimaryKeyRemapper --build-arg DOTNET_CORE_IMAGE=runtime --tag ${docker_repository}tonic-primarykeyremapper:${docker_tag} .

build-tonic-resetschema:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.ResetSchema --build-arg DOTNET_CORE_IMAGE=runtime --tag ${docker_repository}tonic-resetschema:${docker_tag} .

build-tonic-subsetter:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.Subsetter --build-arg DOTNET_CORE_IMAGE=runtime --tag ${docker_repository}tonic-subsetter:${docker_tag} .

build-tonic-tablecount:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.TableCount --build-arg DOTNET_CORE_IMAGE=runtime --tag ${docker_repository}tonic-tablecount:${docker_tag} .

build-tonic-cleanbatch:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.CleanBatch --build-arg DOTNET_CORE_IMAGE=runtime --tag ${docker_repository}tonic-cleanbatch:${docker_tag} .

build-tonic-subsetterwebservice:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.SubsetterWebService --build-arg DOTNET_CORE_IMAGE=aspnet --tag ${docker_repository}tonic-subsetterwebservice:${docker_tag} .

build-tonic-reuserowsalterations:
	docker build --build-arg CONFIGURATION=${configuration} --build-arg PROJECT=Tonic.ReuseRowsAlterations --build-arg DOTNET_CORE_IMAGE=aspnet --tag ${docker_repository}tonic-reuserowsalterations:${docker_tag} .

push-tonic-foreignkeymaskconfigure: build-tonic-foreignkeymaskconfigure
	docker push ${docker_repository}tonic-foreignkeymaskconfigure:${docker_tag}

push-tonic-primarykeyremapper: build-tonic-primarykeyremapper
	docker push ${docker_repository}tonic-primarykeyremapper:${docker_tag}

push-tonic-resetschema: build-tonic-resetschema
	docker push ${docker_repository}tonic-resetschema:${docker_tag}

push-tonic-subsetter: build-tonic-subsetter
	docker push ${docker_repository}tonic-subsetter:${docker_tag}

push-tonic-tablecount: build-tonic-tablecount
	docker push ${docker_repository}tonic-tablecount:${docker_tag}

push-tonic-cleanbatch: build-tonic-cleanbatch
	docker push ${docker_repository}tonic-cleanbatch:${docker_tag}

push-tonic-subsetterwebservice: build-tonic-subsetterwebservice
	docker push ${docker_repository}tonic-subsetterwebservice:${docker_tag}

push-tonic-reuserowsalterations: build-tonic-reuserowsalterations
	docker push ${docker_repository}tonic-reuserowsalterations:${docker_tag}

clean-images:
	docker rmi `docker images -a --filter=dangling=true -q`

clean-tonic-foreignkeymaskconfigure:
	docker rmi ${docker_repository}tonic-foreignkeymaskconfigure:${docker_tag}

clean-tonic-primarykeyremapper:
	docker rmi ${docker_repository}tonic-primarykeyremapper:${docker_tag}

clean-tonic-resetschema:
	docker rmi ${docker_repository}tonic-resetschema:${docker_tag}

clean-tonic-subsetter:
	docker rmi ${docker_repository}tonic-subsetter:${docker_tag}

clean-tonic-tablecount:
	docker rmi ${docker_repository}tonic-tablecount:${docker_tag}

clean-tonic-cleanbatch:
	docker rmi ${docker_repository}tonic-cleanbatch:${docker_tag}

clean-tonic-subsetterwebservice:
	docker rmi ${docker_repository}tonic-subsetterwebservice:${docker_tag}
	
clean-tonic-reuserowsalterations:
	docker rmi ${docker_repository}tonic-reuserowsalterations:${docker_tag}