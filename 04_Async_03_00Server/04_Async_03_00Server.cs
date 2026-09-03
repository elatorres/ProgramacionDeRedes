using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class AsyncTcpServer
{
    // Mantengo una lista de las tareas de clientes
    static List<Task> clientTasks = new List<Task>();
    // El cancellationTokenSource para generar los tokens de cada tarea...
    static CancellationTokenSource shutdownCts = new CancellationTokenSource();
    // El listener...
    static TcpListener listener;

    static async Task Main()
    {
        // mi puerto
        int port = 9000;
        // escucho en cualquiera... y este es mi listener
        listener = new TcpListener(IPAddress.Any, port);
        // y comienzo a oir solicitudes...
        listener.Start();
        Console.WriteLine($"Server listening on port {port}...");

        // Task que va a aceptar las conexiones. le paso el TcpListener y al constructor de
        // cancellationTokens le pido un token. Ese token va a cancelar todas las tareas. 
        var acceptClientsTask = AcceptClientsAsync(listener, shutdownCts.Token);

        // Y me quedo escuchando la consola por un comando de "exit" ...
        while (true)
        {
            string? input = Console.ReadLine();
            // Si es exit aviso que hay que cancelar y terminamos ...
            if (input?.ToLower() == "exit")
            {
                // Aviso que estoy cerrando
                Console.WriteLine("Shutting down server...");
                // detengo la escucha de nuevas conexiones
                listener.Stop();
                // le digo a las tareas que cancelen... se acaba el mundo ....
                // a través del CancellationTokenSource !!
                shutdownCts.Cancel();
                // salgo del loop ...
                break;
            }
        }
        // espero a que terminen... 
        await acceptClientsTask;
        await Task.WhenAll(clientTasks);
        // aviso que se terminó ...
        Console.WriteLine("Server terminated.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    } // fin Main
    
    // AcceptClientsAsync recibe el listener y el cancellatioToken general
    static async Task AcceptClientsAsync(TcpListener listener, CancellationToken token)
    {
        // mientras no se acabe el mundo ....
        while (!token.IsCancellationRequested)
        {
            try
            {
                // si hay solicitud de conexión ...
                var client = await listener.AcceptTcpClientAsync(token);
                // lo conecto y se lo paso a una tarea que lo atienda ...
                Console.WriteLine("Client connected.");
                // Este es el Handler del Cliente. Le paso el TcpCLient y el cancellationToken
                var task = HandleClientAsync(client, token);
                // agrego a la tarea a la lista de tareas en ejecución ...
                clientTasks.Add(task);
                // esto puede servir para varias cosas luego...
            }
            // si hay una excepción de estas salgo del loop y dejo de escuchar conexiones ...
            // revisar que estén todas las excepciones posibles de ocurrir en este contexto...
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or
                SocketException)
            {
                Console.WriteLine("🛑 AcceptClientsAsync stopped accepting clients.");
                break;
            }
        } // fin while
    } // fin AcceptClientsAsync
    
    // HandleClientAsync recibe el TcpClient y el cancellationToken
    // y lo atiende ...
    static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        // atendemos al cliente desde el stream del TcpClient
        using NetworkStream stream = client.GetStream();
        // preparo el buffer para el tamaño del mensaje. Sólo voy a recibir mensajes de texto...
        byte[] lengthBuffer = new byte[4];
        try
        {
            // mientras no se acabe el mundo ...
            while (!token.IsCancellationRequested)
            {
                // leo el mensaje del cliente ... primero el largo ...
                int read = await stream.ReadAsync(lengthBuffer, 0, 4, token);
                // si cero es porque se perdió la comunicación... 
                if (read == 0) // y salgo ...
                {
                    Console.WriteLine("🔌 Client disconnected.");
                    break;
                }
                int msgLength = BitConverter.ToInt32(lengthBuffer, 0);
                // ahora hago un buffer para el mensaje del largo recibido...
                byte[] buffer = new byte[msgLength];
                // y leo el mensaje ... en forma asincrónica
                read = await stream.ReadAsync(buffer, 0, msgLength, token);
                // si cero es porque se perdió la comunicación...
                if (read == 0) // y salgo ...
                {
                    Console.WriteLine("🔌 Client disconnected.");
                    break;
                }
                // decodifico el mensaje 
                string message = Encoding.UTF8.GetString(buffer, 0, read);
                // y lo imprimo en la consola ...
                Console.WriteLine($"Client says: {message}");
                // Ahora le hago el eco para el cliente ...
                // le mando el largo
                await stream.WriteAsync(BitConverter.GetBytes(read));
                // y luego el mensaje
                await stream.WriteAsync(buffer, 0, read, token);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            // se perdió la conexión
            Console.WriteLine("Client disconnected.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"⚠️ IO Error (client disconnect?): {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"⚠️ Socket Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
        }
        finally
        {
            // cierro al cliente
            client.Close();
        }
    } // fin HandleClientAsync
} // fin class AsyncTcpServer
