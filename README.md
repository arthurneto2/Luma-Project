# Luma — Sincronização de relógios de alta precisão

Projeto acadêmico da disciplina de **Sistemas Distribuídos** do curso de Sistemas de Informação da **UFV — Campus Rio Paranaíba**.

Cliente de console em **C# / .NET 10** que sincroniza o relógio local com o relógio de sondas espaciais simuladas por uma API REST, até que o desvio fique **abaixo de 5 ms**, e depois usa o relógio sincronizado para validar uma sequência de jobs.

O tema é o problema clássico de sincronização de relógios em sistemas distribuídos: estimar o desvio (*offset*) e a latência de ida e volta (*RTT*) a partir de quatro timestamps, como faz o NTP.

## O que o cliente faz

1. **Handshake** (`POST /api/start` (ou `/api/start/2` no nível 2)): obtém um token JWT válido por 2 minutos. Se o token expirar (`Unauthorized`), o fluxo inteiro recomeça.
2. **Descoberta** (`GET /api/probe`): lista as sondas, cada uma com seu formato de timestamp e, no nível 2, um fator de dilatação temporal.
3. **Sincronização** (`POST /api/probe/{id}/sync`): repete a troca de timestamps até o desvio ficar abaixo de 5 ms (50.000 ticks).
4. **Jobs** (`POST /api/job/take` e `POST /api/job/{id}/check`): para cada job, envia a hora atual da sonda e o último RTT, até a API responder `Done`.

## Pontos técnicos

**Algoritmo NTP adaptado.** Com os instantes `t0` (envio local), `t1` (recebimento na sonda), `t2` (resposta da sonda) e `t3` (recebimento local):

```
offset = ((t1 - t0) + (t2 - t3)) / 2
rtt    = (t3 - t0) - (t2 - t1)
```

O offset é acumulado entre as chamadas até convergir para menos de 5 ms.

**Dilatação temporal (nível 2).** O relógio de cada sonda avança a uma taxa diferente da do relógio local. O tempo decorrido localmente é dividido pelo fator da sonda antes de projetar a hora dela, tanto durante a sincronização quanto entre uma sincronização e o envio de um job.

**Falhas transitórias.** No nível 2, a sincronização pode responder `ProbeUnreachable`; o cliente espera 5 segundos e tenta de novo. Falhas de autenticação reiniciam o fluxo a partir do handshake.

**Quatro formatos de timestamp.** Cada sonda usa um formato. Uma interface comum (`ITimestampEncoder`) com uma fábrica (`EncoderFactory`) escolhe a implementação a partir do nome informado pela API:

| Codificador | Formato |
|---|---|
| `Iso8601Encoder` | texto ISO 8601 com fuso (`yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz`) |
| `TicksPureEncoder` | contagem de ticks (1 tick = 100 ns) como texto |
| `TicksBinaryEncoder` | inteiro de 64 bits em Little-Endian, em Base64 |
| `TicksBinaryBigEndianEncoder` | inteiro de 64 bits em Big-Endian, em Base64 |

## Estrutura

```
Luma.Client/
├── Program.cs                 orquestra handshake, sincronização e jobs; reinicia o fluxo em caso de falha
├── Services/
│   ├── LumaApiClient.cs       HttpClient com token Bearer e chamadas tipadas à API
│   └── TimeSyncService.cs     estado de cada sonda, cálculo de offset/RTT e dilatação temporal
├── Encoders/                  ITimestampEncoder, EncoderFactory e os quatro codificadores
└── Models/Dtos.cs             records imutáveis de requisição e resposta
```

## Como executar

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download) e acesso à internet (a API fica em `https://luma.lacuna.cc`).

A API exige um nome e um e-mail no handshake. Informe-os por variáveis de ambiente; se não estiverem definidas, o programa pergunta no console. Nenhuma credencial fica no código.

```bash
# Linux / macOS
export LUMA_USERNAME="Seu Nome"
export LUMA_EMAIL="voce@exemplo.com"

# Windows (PowerShell)
$env:LUMA_USERNAME = "Seu Nome"
$env:LUMA_EMAIL = "voce@exemplo.com"
```

Depois, na raiz do repositório:

```bash
dotnet run --project Luma.Client            # pergunta o nível
dotnet run --project Luma.Client level1     # nível 1: sincronização simples
dotnet run --project Luma.Client level2     # nível 2: dilatação temporal e falhas transitórias
```

O console mostra cada etapa (handshake, sincronização de cada sonda, validação dos jobs) e termina com a mensagem de sucesso quando todos os jobs são validados.

## Autor

Arthur Roberto de Paula Neto — [GitHub](https://github.com/arthurneto2) · [LinkedIn](https://linkedin.com/in/arthurneto-dev)
