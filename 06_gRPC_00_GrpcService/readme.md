## Aplicación de gRPC entre un cliente y un servidor.
Versión Asincrónica

Instalar
* Google.Protobuf
* Grpc.AspNetCore (**Cliente no**)
* Grpc.Net.Client
* Grpc.Tools

Este es el **"Greeter Example"** con comentarios

Verificar que en Server.csproj y en Client.csproj está la referencia a Protos/auth.proto o similar.
Por ejemplo:

>  \<ItemGroup><br>
>  \<Protobuf Include="Protos/grpcservice.proto" GrpcServices=**"Client"** /><br>
>  \</ItemGroup>

La del cliente debe decir **"Client"** y la del servidor **"Server"**.

En el servidor poner:

> using Microsoft.AspNetCore.Hosting;<br>
> using Microsoft.Extensions.Hosting;<br>
> using System;<br>
> using System.Collections.Generic;<br> 
> using System.IO;<br>
> using System.Linq;<br>
> using System.Threading.Tasks;<br>

Y en el cliente

> using System;<br>
> using System.Threading.Tasks;<br>
> using Grpc.Net.Client;<br>

Asegurar que en el proto existe: 
> option csharp_namespace = "Cliente_gRPC";

Y en el archivo Protos/*.proto coincide efectivamente con el namespace del cliente y el servidor respectivamente.

En el Servidor en **Properties/launchSettings.json** 
Poner el **"schema": ..., "profiles": { "http": { ... "applicationUrl": "http://localhost:5000", ...**
el puerto que debe usar el servidor.

El fuente en Services (servidor), la declaración en el .proto, y el Program.cs o el que tenga el Main deben estar en el mismo namespace.
La declaración en Services dice cómo responde el Servidor a cada solicitud de Servicio.

