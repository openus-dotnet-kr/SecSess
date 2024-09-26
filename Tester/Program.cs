﻿using Openus.Net.SecSess.Key;
using Openus.Net.SecSess.Secure.Algorithm;
using Openus.Net.SecSess.Tcp;
using System.Net;

internal class Program
{
    private const int Retry = 100;

    private static void Main(string[] args)
    {
        PublicKey pubkey = PublicKey.Load(Asymmetric.RSA, "test.pub");
        PrivateKey privkey = PrivateKey.Load(Asymmetric.RSA, "test.priv");

        Set set = new Set()
        {
            Asymmetric = Asymmetric.RSA,
            Symmetric = Symmetric.AES,
            Hash = Hash.SHA256,
        };

        new Thread(() =>
        {
            for (int re = 0; re < Retry; re++)
            {
                Server server = Server.Create(IPEndPoint.Parse($"127.0.0.1:12345"), privkey, set);
                server.Start();

                Server.Client sclient = server.AcceptClient();

                for (int i = 0; i < 100; i++)
                {
                    byte[] buffer = sclient.Read();
                    sclient.Write(buffer);

                    sclient.FlushStream();
                }

                server.Stop();
            }
        }).Start();
        new Thread(async () =>
        {
            for (int re = 0; re < Retry; re++)
            {
                DateTime time1 = DateTime.Now;

                Client client = Client.Create(pubkey, set);
                await client.ConnectAsync(IPEndPoint.Parse($"127.0.0.1:12345"));

                TimeSpan span1 = DateTime.Now - time1;

                byte[] buffer = new byte[1024];
                new Random().NextBytes(buffer);

                byte[] check = (byte[])buffer.Clone();

                DateTime time2 = DateTime.Now;

                for (int i = 0; i < 100; i++)
                {
                    await client.WriteAsync(buffer);
                    buffer = client.Read();

                    client.FlushStream();
                }

                TimeSpan span2 = DateTime.Now - time2;

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] != check[i])
                        throw new Exception("buffer is corrupted");
                }

                if ((re - 9) % 10 == 0)
                {
                    Console.WriteLine($"{re + 1}. Con. Total: {span1.TotalSeconds}s");
                    Console.WriteLine($"{re + 1}. Com. Total: {span2.TotalSeconds}s");
                }

                client.Close();
            }
        }).Start();
    }
}

/*
        //var pair = KeyPair.GenerateRSA();
        //pair.PublicKey.Save("test.pub");
        //pair.PrivateKey.Save("test.priv");

        Set set = new Set()
        {
            Asymmetric = Asymmetric.RSA,
            Symmetric = Symmetric.AES,
            Hash = Hash.SHA256,
        };

        string RoleAndType = args[0];
        int Size = 1 << int.Parse(args[1]);
        int Repeat = int.Parse(args[2]);
        string Ip = args[3];

        int port = 1234;

        List<double> ConnectTotals = new List<double>();
        List<double> CommunicateTotals = new List<double>();

        PublicKey pubkey = PublicKey.Load(Asymmetric.RSA, "test.pub");
        PrivateKey privkey = PrivateKey.Load(Asymmetric.RSA, "test.priv");

        switch (RoleAndType)
        {
            case "ss":
                for (int re = 0; re < Retry; re++)
                {
                    Server server = Server.Create(IPEndPoint.Parse($"{Ip}:{port}"), privkey, set);
                    server.Start();

                    Server.Client sclient = server.AcceptClient();

                    for (int i = 0; i < Repeat; i++)
                    {
                        byte[] buffer = sclient.Read();
                        sclient.Write(buffer);

                        sclient.FlushStream();
                    }

                    server.Stop();
                }

                break;

            case "sc":
                for (int re = 0; re < Retry; re++)
                {
                    DateTime time1 = DateTime.Now;

                    Client client = Client.Create(pubkey, set);
                    client.Connect(IPEndPoint.Parse($"{Ip}:{port}"));

                    TimeSpan span1 = DateTime.Now - time1;

                    byte[] buffer = new byte[Size];
                    new Random().NextBytes(buffer);

                    byte[] check = (byte[])buffer.Clone();

                    DateTime time2 = DateTime.Now;

                    for (int i = 0; i < Repeat; i++)
                    {
                        client.Write(buffer);
                        buffer = client.Read();

                        client.FlushStream();
                    }

                    TimeSpan span2 = DateTime.Now - time2;

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] != check[i])
                            throw new Exception("buffer is corrupted");
                    }

                    if ((re - 9) % 10 == 0)
                    {
                        Console.WriteLine($"{re + 1}. Con. Total: {span1.TotalSeconds}s");
                        Console.WriteLine($"{re + 1}. Com. Total: {span2.TotalSeconds}s");
                    }
                    if (re != 0)
                    {
                        ConnectTotals.Add(span1.TotalSeconds);
                        CommunicateTotals.Add(span2.TotalSeconds);
                    }

                    client.Close();
                }

                using (StreamWriter sw = new StreamWriter($"output.txt", true))
                {
                    sw.WriteLine(JsonSerializer.Serialize(new
                    {
                        RoleAndType,
                        Size,
                        Repeat,
                        ConAverage = ConnectTotals.Sum() / Retry,
                        ComAverage = CommunicateTotals.Sum() / Retry,
                    }));
                }

                break;

            case "ns":
                for (int re = 0; re < Retry; re++)
                {
                    TcpListener server = new TcpListener(IPEndPoint.Parse($"{Ip}:{port}"));
                    server.Start();

                    TcpClient sclient = server.AcceptTcpClient();

                    for (int i = 0; i < Repeat; i++)
                    {
                        byte[] buffer = new byte[Size];

                        int s = 0;
                        while (s < Size) s += sclient.GetStream().Read(buffer, s, Size - s);
                        sclient.GetStream().Write(buffer);

                        sclient.GetStream().Flush();
                    }

                    server.Stop();
                }

                break;

            case "nc":
                for (int re = 0; re < Retry; re++)
                {
                    DateTime time1 = DateTime.Now;

                    TcpClient client = new TcpClient();
                    client.Connect(IPEndPoint.Parse($"{Ip}:{port}"));

                    TimeSpan span1 = DateTime.Now - time1;

                    byte[] buffer = new byte[Size];
                    new Random().NextBytes(buffer);

                    byte[] check = (byte[])buffer.Clone();

                    DateTime time2 = DateTime.Now;

                    for (int i = 0; i < Repeat; i++)
                    {
                        client.GetStream().Write(buffer);

                        int s = 0;
                        while (s < Size) s += client.GetStream().Read(buffer, s, Size - s);

                        client.GetStream().Flush();
                    }

                    TimeSpan span2 = DateTime.Now - time2;

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] != check[i])
                            throw new Exception("buffer is corrupted");
                    }

                    if ((re - 9) % 10 == 0)
                    {
                        Console.WriteLine($"{re + 1}. Con. Total: {span1.TotalSeconds}s");
                        Console.WriteLine($"{re + 1}. Com. Total: {span2.TotalSeconds}s");
                    }
                    if (re != 0)
                    {
                        ConnectTotals.Add(span1.TotalSeconds);
                        CommunicateTotals.Add(span2.TotalSeconds);
                    }

                    client.Close();
                }

                using (StreamWriter sw = new StreamWriter($"output.txt", true))
                {
                    sw.WriteLine(JsonSerializer.Serialize(new
                    {
                        RoleAndType,
                        Size,
                        Repeat,
                        ConAverage = ConnectTotals.Sum() / Retry,
                        ComAverage = CommunicateTotals.Sum() / Retry,
                    }));
                }

                break;
        }
 */