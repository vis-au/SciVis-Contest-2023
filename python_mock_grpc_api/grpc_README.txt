generate python code stuff:
run:
	pip install grpcio-tools

from the scivis-contest-2023 directory execute:
	python -m grpc_tools.protoc -I=protos --python_out=python_mock_grpc_api/ --pyi_out=python_out=python_mock_grpc_api/ --grpc_python_out=python_mock_grpc_api/ protos/brain.proto      


generate C# stuff:
if Source/Plugins is empty:
	download C# unity zip from https://packages.grpc.io/archive/2019/11/6950e15882f28e43685e948a7e5227bfcef398cd-6d642d6c-a6fc-4897-a612-62b0a3c9026b/index.xml
	extract contents of Plugin folder to Source/Assets/plugins
	also
	download package from https://www.nuget.org/packages/Grpc.Tools/2.26.0
	extract the \tools\windows_x64\grpc_csharp_plugin.exe file to Source/Assets/Plugins/

	also if you run into a Google.Protobuf namespace error regarding IProtoBuffer or something. Download the following package, rename to zip and replace the original Plugins/Google.Protobuf/lib/net45 folder with the netstandard2.0 folder
	https://www.nuget.org/packages/Google.Protobuf/
	see post (https://medium.com/@megatran23/for-those-who-ran-into-the-issue-the-type-or-namespace-name-ibuffermessage-does-not-exist-in-the-62fc347cfba7)

from the scivis-contest-2023 directory execute:
protoc -I=protos/ --csharp_out=Source/Assets/SciVis/Repository --grpc_out=Source/Assets/SciVis/Repository --plugin=protoc-gen-grpc=Source/Assets/Plugins/grpc_csharp_plugin.exe  brain.proto