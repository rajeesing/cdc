﻿using ConsumeCdc.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

var connectionString = CreateConnectionString();
var pollingIntervalMs = 1000;
var tables = new List<string> { "dbo_Employee" };
using var cdcCancellation = new CancellationTokenSource();
var cdcCancellationToken = cdcCancellation.Token;

var changeDataChannel = Channel.CreateUnbounded<IReadOnlyCollection<AllChangeRow>>();
_ = Task.Run(async () =>
{
    cdcCancellationToken.ThrowIfCancellationRequested();
    var lowBoundLsn = await GetStartLsn(connectionString);
    while (true)
    {
        using var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();

            var highBoundLsn = await Cdc.GetMaxLsnAsync(connection);
            if (lowBoundLsn <= highBoundLsn)
            {
                Console.WriteLine($"Polling from '{lowBoundLsn}' to '{highBoundLsn}'.");

                var changes = new List<AllChangeRow>();
                foreach (var table in tables)
                {
                    var changeSets = await Cdc.GetAllChangesAsync(
                        connection, table, lowBoundLsn, highBoundLsn, AllChangesRowFilterOption.AllUpdateOld);
                    changes.AddRange(changeSets);
                }

                var orderedChanges = changes.OrderBy(x => x.SequenceValue).ToList();
                await changeDataChannel.Writer.WriteAsync(orderedChanges);

                lowBoundLsn = await Cdc.GetNextLsnAsync(connection, highBoundLsn);
            }
            else
            {
                Console.WriteLine($"No changes since last poll '{lowBoundLsn}'.");
            }
        }
        catch (OperationCanceledException)
        {
            // We mark the channel as completed to notify that all consumers should
            // read the last elements and stop.
            changeDataChannel.Writer.Complete();
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            changeDataChannel.Writer.Complete();
            break;
        }

        await Task.Delay(pollingIntervalMs, cdcCancellationToken);
    }
});

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters =
            {
                new JsonStringEnumConverter()
            }
};

Console.WriteLine("Starting consuming changes...");
await foreach (var changes in changeDataChannel.Reader.ReadAllAsync())
{
    var changeDataJson = JsonSerializer.Serialize(changes, options);
    Console.WriteLine(changeDataJson + "\n");
}

static async Task<BigInteger> GetStartLsn(string connectionString)
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var currentMaxLsn = await Cdc.GetMaxLsnAsync(connection);
    return await Cdc.GetNextLsnAsync(connection, currentMaxLsn);
}

static string CreateConnectionString()
{
    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
    builder.DataSource = "<SQL Server Instance Name>";
    builder.UserID = "<User Name>";
    builder.Password = "<Password>";
    builder.InitialCatalog = "<Table Name>";
    builder.Encrypt = false;
    return builder.ConnectionString;
}
