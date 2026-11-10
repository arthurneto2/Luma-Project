using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Luma.Client.Models;
using Luma.Client.Services;

namespace Luma.Client;

class Program
{
    private enum FlowStatus
    {
        Completed,
        Restart
    }

    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=================================================");
        Console.WriteLine("🚀 PROJETO LUMA - SINCRONIZAÇÃO DE RELÓGIOS ESPACIAIS");
        Console.WriteLine("=================================================");
        Console.ResetColor();

        // 1. Determine execution level
        bool level2 = false;
        bool skipPrompt = false;

        if (args.Length > 0)
        {
            if (args[0].Equals("level2", StringComparison.OrdinalIgnoreCase))
            {
                level2 = true;
                skipPrompt = true;
            }
            else if (args[0].Equals("level1", StringComparison.OrdinalIgnoreCase))
            {
                level2 = false;
                skipPrompt = true;
            }
        }

        if (!skipPrompt)
        {
            Console.Write("Selecione o nível de execução [1 ou 2] (Padrão 1): ");
            var input = Console.ReadLine();
            if (input == "2")
            {
                level2 = true;
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Executando no NÍVEL {(level2 ? "2 (Avançado - Ventos Solares e Relatividade)" : "1 (Padrão)")}");
        Console.ResetColor();

        // 2. Credenciais: variáveis de ambiente LUMA_USERNAME e LUMA_EMAIL, ou pergunta no console.
        //    Nada fica fixo no código.
        var username = ReadCredential("LUMA_USERNAME", "Nome de usuário");
        var email = ReadCredential("LUMA_EMAIL", "E-mail");

        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine("-------------------------------------------------");

        using var apiClient = new LumaApiClient();
        int attempt = 1;

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\n[Tentativa #{attempt}] Iniciando fluxo completo...");
            Console.ResetColor();

            try
            {
                var status = await RunOrchestratorAsync(apiClient, username, email, level2);
                if (status == FlowStatus.Completed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n🎉 Desafio concluído com SUCESSO!");
                    Console.WriteLine("=================================================");
                    Console.ResetColor();
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Erro durante a execução: {ex.Message}");
                Console.ResetColor();
            }

            attempt++;
            Console.WriteLine("Aguardando 3 segundos antes de reiniciar o fluxo...");
            await Task.Delay(3000);
        }
    }

    private static string ReadCredential(string envVar, string label)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        while (true)
        {
            Console.Write($"{label} (ou defina a variável {envVar}): ");
            var input = Console.ReadLine();

            if (input is null)
            {
                throw new InvalidOperationException(
                    $"{label} não informado. Defina a variável de ambiente {envVar}.");
            }

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }
        }
    }

    private static async Task<FlowStatus> RunOrchestratorAsync(LumaApiClient apiClient, string username, string email, bool level2)
    {
        // Step 1: Handshake
        Console.WriteLine("Realizando handshake inicial (POST /api/start)...");
        var startResponse = await apiClient.StartAsync(username, email, level2);
        
        if (startResponse.Code != "Success" || string.IsNullOrEmpty(startResponse.AccessToken))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro no handshake: {startResponse.Code} - {startResponse.Message}");
            Console.ResetColor();
            return FlowStatus.Restart;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Handshake realizado com sucesso!");
        Console.ResetColor();

        // Dynamically update the HTTP client token as requested
        apiClient.ActiveToken = startResponse.AccessToken;

        // Step 2: Retrieve Probes
        Console.WriteLine("Buscando lista de sondas espaciais (GET /api/probe)...");
        var probesResponse = await apiClient.GetProbesAsync();
        
        if (probesResponse.Code != "Success" || probesResponse.Probes == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro ao obter sondas: {probesResponse.Code} - {probesResponse.Message}");
            Console.ResetColor();
            
            if (probesResponse.Code == "Unauthorized")
                return FlowStatus.Restart;

            return FlowStatus.Restart;
        }

        Console.WriteLine($"Encontrada(s) {probesResponse.Probes.Length} sonda(s):");
        var syncServices = new List<TimeSyncService>();
        foreach (var p in probesResponse.Probes)
        {
            Console.WriteLine($" - {p.Name} (ID: {p.Id}, Encoding: {p.Encoding}, Fator Dilatação: {p.TimeDilationFactor ?? 1.0})");
            syncServices.Add(new TimeSyncService(p));
        }

        // Step 3: Synchronize clocks for each probe
        Console.WriteLine("\nIniciando sincronização dos relógios...");
        foreach (var syncService in syncServices)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\nSincronizando relógio com sonda: {syncService.ProbeName}");
            Console.ResetColor();

            int syncAttempts = 0;
            while (!syncService.IsSynchronized)
            {
                syncAttempts++;
                Console.Write($"Chamada de Sincronização #{syncAttempts}... ");
                
                var (syncResponse, t0, t3) = await apiClient.SyncProbeAsync(syncService.ProbeId);

                if (syncResponse.Code == "ProbeUnreachable")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Indisponível (Ventos Solares). Aguardando 5 segundos para tentar novamente...");
                    Console.ResetColor();
                    await Task.Delay(5000);
                    continue;
                }

                if (syncResponse.Code == "Unauthorized" || syncResponse.Code == "Fail")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Falha na sincronização: {syncResponse.Code} - {syncResponse.Message}");
                    Console.ResetColor();
                    return FlowStatus.Restart;
                }

                if (syncResponse.Code == "Success")
                {
                    long t1 = syncService.Encoder.Decode(syncResponse.T1);
                    long t2 = syncService.Encoder.Decode(syncResponse.T2);

                    syncService.UpdateSync(t0, t1, t2, t3);

                    long currentOffsetMs = syncService.AccumulatedOffset / 10000;
                    Console.WriteLine($"Sucesso. Offset acumulado: {currentOffsetMs} ms. Round-trip: {syncService.LastRoundTrip / 10000} ms.");

                    if (syncService.IsSynchronized)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Sonda {syncService.ProbeName} SINCRONIZADA com sucesso! (BaseEarthTicks capturado em t3: {syncService.BaseEarthTicks})");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Código de resposta inesperado: {syncResponse.Code}");
                    Console.ResetColor();
                    return FlowStatus.Restart;
                }
            }
        }

        // Step 4: Job Consumer Loop
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\nIniciando consumo de Jobs...");
        Console.ResetColor();

        string lastCheckCode = "";

        while (true)
        {
            Console.WriteLine("Obtendo job (POST /api/job/take)...");
            var jobResponse = await apiClient.TakeJobAsync();

            if (jobResponse.Code == "Unauthorized" || jobResponse.Code == "Fail")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro ao obter Job: {jobResponse.Code} - {jobResponse.Message}");
                Console.ResetColor();
                return FlowStatus.Restart;
            }

            if (jobResponse.Job == null)
            {
                Console.WriteLine("Recebido job nulo. Encerrando loop de consumo de jobs.");
                if (lastCheckCode == "Done")
                {
                    return FlowStatus.Completed;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Status do último check foi '{lastCheckCode}' (esperado 'Done'). Verificando se terminamos...");
                    Console.ResetColor();
                    // If job is null and we don't have Done, let's treat it as completed if API signals no more work
                    return FlowStatus.Completed;
                }
            }

            var job = jobResponse.Job;
            Console.WriteLine($"Job recebido: ID {job.Id} para sonda {job.ProbeName}");

            // Find matching sync service
            var syncService = syncServices.FirstOrDefault(s => s.ProbeName.Equals(job.ProbeName, StringComparison.OrdinalIgnoreCase));
            if (syncService == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro: Sonda '{job.ProbeName}' do Job não encontrada nas sondas sincronizadas.");
                Console.ResetColor();
                return FlowStatus.Restart;
            }

            // Get current adjusted synced time and format it
            long probeNowTicks = syncService.GetCurrentProbeTicks();
            string probeNowEncoded = syncService.Encoder.Encode(probeNowTicks);
            long roundTrip = syncService.LastRoundTrip;

            Console.WriteLine($"Validando Job: ProbeNow = {probeNowEncoded} (Ticks: {probeNowTicks}), RoundTrip = {roundTrip / 10000} ms ({roundTrip} ticks)");

            var checkResponse = await apiClient.CheckJobAsync(job.Id, probeNowEncoded, roundTrip);
            lastCheckCode = checkResponse.Code;

            Console.WriteLine($"Resultado do check: Code = {checkResponse.Code}, Message = {checkResponse.Message}");

            if (checkResponse.Code == "Done")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Servidor retornou código 'Done'.");
                Console.ResetColor();
                return FlowStatus.Completed;
            }

            if (checkResponse.Code == "Fail" || checkResponse.Code == "Unauthorized")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Validação falhou ou expirou com código: {checkResponse.Code}");
                Console.ResetColor();
                return FlowStatus.Restart;
            }

            if (checkResponse.Code != "Success")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Código de resposta inesperado do check: {checkResponse.Code}");
                Console.ResetColor();
            }
        }
    }
}