#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Server
{
    public class Server
    {
        public static void Main(string[] args)
        {
            // Creates and starts running the gRPC host/webserver with Run().
            CreateHostBuilder(args).Build().Run();
            // Synchronic version has to be closed with Ctrl-C
        }
        
        // Sets up a gRPC web server using ASP.NET Core’s hosting system.
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Calls UseStartup<Startup>() to define the startup behavior
                    // (how services and endpoints are configured).
                    webBuilder.UseStartup<Startup>();
                });
    }
}