using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NetArcaWs.IntegrationTests;

public sealed record ProtectedHomologationSecretRead(string? EncodedValue, bool Exists, DateTimeOffset? UpdatedAtUtc)
{
    public override string ToString() => "ProtectedHomologationSecretRead { redacted }";
}

public interface IProtectedHomologationStateStore
{
    Task<ProtectedHomologationSecretRead> ReadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(string encodedValue, CancellationToken cancellationToken = default);
}

/// <summary>Reads the Actions-injected secret and updates its protected environment value through gh.</summary>
public sealed class ProtectedHomologationStateStore : IProtectedHomologationStateStore
{
    public const string SecretName = "HOMOLOGATION_STATE";
    public const string TokenVariable = "HOMOLOGATION_STATE_TOKEN";
    private const string EnvironmentName = "homologacion";
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
    private readonly string repository;
    private readonly string? token;
    private readonly string? injectedState;

    private ProtectedHomologationStateStore(string repository, string? token, string? injectedState)
    {
        this.repository = repository;
        this.token = token;
        this.injectedState = injectedState;
    }

    public static ProtectedHomologationStateStore FromEnvironment(string repository)
    {
        ValidateRepository(repository);
        string? token = Environment.GetEnvironmentVariable(TokenVariable);
        string? injectedState = Environment.GetEnvironmentVariable(SecretName);
        Environment.SetEnvironmentVariable(TokenVariable, null);
        Environment.SetEnvironmentVariable(SecretName, null);
        return new(repository, token, injectedState);
    }

    public async Task<ProtectedHomologationSecretRead> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Protected homologation state cannot be checked: HOMOLOGATION_STATE_TOKEN is unavailable.");

        string output = await RunGhAsync(
            ["secret", "list", "--env", EnvironmentName, "--repo", repository, "--json", "name,updatedAt"],
            stdin: null, cancellationToken);
        try
        {
            using JsonDocument document = JsonDocument.Parse(output);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw StateStoreFailure();

            JsonElement? match = null;
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (item.GetProperty("name").GetString() == SecretName)
                {
                    if (match is not null) throw StateStoreFailure();
                    match = item;
                }
            }
            if (match is null)
                return new(injectedState, false, null);

            JsonElement updatedAt = match.Value.GetProperty("updatedAt");
            if (updatedAt.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(updatedAt.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
                throw StateStoreFailure();
            return new(injectedState, true, parsed);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw StateStoreFailure();
        }
    }

    public async Task SaveAsync(string encodedValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Protected homologation state cannot be written: HOMOLOGATION_STATE_TOKEN is unavailable.");
        if (encodedValue.Length > ProtectedHomologationStateSession.MaximumEncodedLength)
            throw new InvalidOperationException("Protected homologation state exceeds the GitHub secret size limit.");

        _ = await RunGhAsync(
            ["secret", "set", SecretName, "--env", EnvironmentName, "--repo", repository],
            encodedValue, cancellationToken);
    }

    private async Task<string> RunGhAsync(IReadOnlyList<string> arguments, string? stdin, CancellationToken cancellationToken)
    {
        try
        {
            using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            bounded.CancelAfter(CommandTimeout);
            cancellationToken = bounded.Token;
            var start = new ProcessStartInfo("gh")
            {
                RedirectStandardInput = stdin is not null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (string argument in arguments) start.ArgumentList.Add(argument);

            // Keep the persistence credential scoped to gh. Do not pass ARCA or alternate GitHub tokens.
            foreach (string name in start.Environment.Keys.Where(name =>
                         name.StartsWith("ARCA_", StringComparison.Ordinal) ||
                         name.StartsWith("WSAA_", StringComparison.Ordinal) ||
                         name.StartsWith("HOMO_", StringComparison.Ordinal) ||
                         name.StartsWith("HOMOLOGATION_", StringComparison.Ordinal) ||
                         name is "GITHUB_TOKEN" or "GH_TOKEN").ToArray())
                start.Environment.Remove(name);
            start.Environment["GH_TOKEN"] = token;
            start.Environment["GH_PROMPT_DISABLED"] = "1";

            using var process = Process.Start(start) ?? throw StateStoreFailure();
            try
            {
                Task<string> stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
                Task<string> stderr = process.StandardError.ReadToEndAsync(cancellationToken);
                if (stdin is not null)
                {
                    await process.StandardInput.WriteAsync(stdin.AsMemory(), cancellationToken);
                    process.StandardInput.Close();
                }
                await process.WaitForExitAsync(cancellationToken);
                string output = await stdout;
                _ = await stderr; // Drain and discard diagnostics; gh may include secret-related context.
                if (process.ExitCode != 0) throw StateStoreFailure();
                return output;
            }
            catch
            {
                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                }
                throw;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw StateStoreFailure();
        }
    }

    private static void ValidateRepository(string repository)
    {
        string[] parts = repository.Split('/');
        if (parts.Length != 2 || parts.Any(part => part.Length == 0 || part.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'))))
            throw new ArgumentException("A valid OWNER/REPO value is required.", nameof(repository));
    }

    private static InvalidOperationException StateStoreFailure()
        => new("Protected homologation state could not be read or written through GitHub.");
}

public sealed class ProtectedHomologationStateSession
{
    internal const int MaximumEncodedLength = 48 * 1024;
    private const int MaximumDecompressedLength = 4 * 1024 * 1024;
    private static readonly TimeSpan MetadataTolerance = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 16 };

    private readonly IProtectedHomologationStateStore store;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim gate = new(1, 1);
    private ProtectedHomologationStateBundle state;
    private bool poisoned;

    private ProtectedHomologationStateSession(
        IProtectedHomologationStateStore store,
        ProtectedHomologationStateBundle state,
        TimeProvider clock)
    {
        this.store = store;
        this.state = state;
        this.clock = clock;
    }

    public static async Task<ProtectedHomologationStateSession> OpenAsync(
        IProtectedHomologationStateStore store,
        CancellationToken cancellationToken = default,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        clock ??= TimeProvider.System;
        ProtectedHomologationSecretRead read = await store.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!read.Exists)
        {
            if (!string.IsNullOrEmpty(read.EncodedValue))
                throw new InvalidOperationException("Protected homologation state was injected but no matching environment secret exists.");
            var initial = new ProtectedHomologationStateBundle(1, clock.GetUtcNow(), [], [], null);
            var session = new ProtectedHomologationStateSession(store, initial, clock);
            await session.PersistAsync(initial, cancellationToken).ConfigureAwait(false);
            return session;
        }

        if (string.IsNullOrWhiteSpace(read.EncodedValue) || read.UpdatedAtUtc is null)
            throw new InvalidOperationException("The existing protected homologation state secret was not injected or has malformed metadata.");

        ProtectedHomologationStateBundle state = Decode(read.EncodedValue);
        DateTimeOffset now = clock.GetUtcNow();
        if (read.UpdatedAtUtc > state.SavedAtUtc + MetadataTolerance ||
            state.SavedAtUtc > read.UpdatedAtUtc.Value + MetadataTolerance ||
            state.SavedAtUtc > now + MetadataTolerance)
            throw new InvalidOperationException("The injected protected homologation state is stale or has an invalid timestamp.");
        ValidateState(state);
        return new(store, state, clock);
    }

    public async Task<ProtectedHomologationTicketState?> FindTicketAsync(string identityKey, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureUsable();
            return state.Tickets.SingleOrDefault(ticket => ticket.IdentityKey == identityKey);
        }
        finally { gate.Release(); }
    }

    public Task RecordLoginStartedAsync(string identityKey, DateTimeOffset startedAtUtc, CancellationToken cancellationToken = default)
        => UpdateGuardAsync(identityKey, startedAtUtc, startedAtUtc.AddMinutes(11), cancellationToken);

    public Task RecordLoginCompletedAsync(
        string identityKey,
        DateTimeOffset completedAtUtc,
        string? ticketIdentityKey,
        string? ticketXml,
        CancellationToken cancellationToken = default)
        => UpdateCompletedAsync(identityKey, completedAtUtc, ticketIdentityKey, ticketXml, cancellationToken);

    public async Task<ProtectedHomologationLoginGuardState?> FindLoginGuardAsync(string identityKey, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { EnsureUsable(); return state.LoginGuards.SingleOrDefault(item => item.IdentityKey == identityKey); }
        finally { gate.Release(); }
    }

    private async Task UpdateGuardAsync(
        string identityKey,
        DateTimeOffset lastAttemptUtc,
        DateTimeOffset cooldownUntilUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityKey);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureUsable();
            var guard = new ProtectedHomologationLoginGuardState(identityKey, lastAttemptUtc, cooldownUntilUtc);
            var guards = state.LoginGuards.Where(item => item.IdentityKey != identityKey).Append(guard).ToList();
            await PersistAsync(state with { LoginGuards = guards }, cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    private async Task UpdateCompletedAsync(string guardKey, DateTimeOffset completedAtUtc, string? ticketKey, string? ticketXml, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guardKey);
        if ((ticketKey is null) != (ticketXml is null)) throw new ArgumentException("Ticket key and XML must be supplied together.");
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureUsable();
            var guard = new ProtectedHomologationLoginGuardState(guardKey, completedAtUtc, completedAtUtc.AddMinutes(10));
            var guards = state.LoginGuards.Where(item => item.IdentityKey != guardKey).Append(guard).ToList();
            List<ProtectedHomologationTicketState> tickets = state.Tickets;
            if (ticketKey is not null)
                tickets = tickets.Where(item => item.IdentityKey != ticketKey).Append(new(ticketKey, ticketXml!)).ToList();
            await PersistAsync(state with { LoginGuards = guards, Tickets = tickets }, cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    public async Task<byte[]?> GetJournalSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureUsable();
            if (state.JournalSnapshotBase64 is null) return null;
            byte[] bytes;
            try { bytes = Convert.FromBase64String(state.JournalSnapshotBase64); }
            catch (FormatException) { throw InvalidBundle(); }
            if (bytes.Length > MaximumDecompressedLength) throw InvalidBundle();
            return bytes;
        }
        finally { gate.Release(); }
    }

    public async Task StoreJournalSnapshotAsync(byte[] snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureUsable();
            await PersistAsync(state with { JournalSnapshotBase64 = Convert.ToBase64String(snapshot) }, cancellationToken).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    private async Task PersistAsync(ProtectedHomologationStateBundle candidate, CancellationToken cancellationToken)
    {
        try
        {
            candidate = candidate with { SavedAtUtc = clock.GetUtcNow() };
            ValidateState(candidate);
            string encoded = Encode(candidate);
            using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            bounded.CancelAfter(TimeSpan.FromSeconds(30));
            await store.SaveAsync(encoded, bounded.Token).ConfigureAwait(false);
            state = candidate;
        }
        catch (Exception)
        {
            poisoned = true;
            throw new InvalidOperationException("Protected homologation state could not be persisted; the session is closed to further ARCA calls.");
        }
    }

    private void EnsureUsable()
    {
        if (poisoned)
            throw new InvalidOperationException("Protected homologation state is unavailable after a persistence failure; no further ARCA calls are allowed.");
    }

    private static string Encode(ProtectedHomologationStateBundle bundle)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(bundle, JsonOptions);
        if (json.Length > MaximumDecompressedLength) throw InvalidBundle();
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(json);
        string encoded = Convert.ToBase64String(compressed.ToArray());
        if (encoded.Length > MaximumEncodedLength)
            throw new InvalidOperationException("Protected homologation state exceeds the 48 KiB GitHub secret limit.");
        return encoded;
    }

    private static ProtectedHomologationStateBundle Decode(string encoded)
    {
        if (encoded.Length > MaximumEncodedLength) throw InvalidBundle();
        try
        {
            byte[] compressed = Convert.FromBase64String(encoded);
            using var input = new MemoryStream(compressed, writable: false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            byte[] buffer = new byte[8192];
            int read;
            while ((read = gzip.Read(buffer, 0, buffer.Length)) != 0)
            {
                if (output.Length + read > MaximumDecompressedLength) throw InvalidBundle();
                output.Write(buffer, 0, read);
            }
            var state = JsonSerializer.Deserialize<ProtectedHomologationStateBundle>(output.ToArray(), JsonOptions)
                ?? throw InvalidBundle();
            ValidateState(state);
            return state;
        }
        catch (Exception exception) when (exception is FormatException or InvalidDataException or JsonException or ArgumentException)
        {
            throw InvalidBundle();
        }
    }

    private static void ValidateState(ProtectedHomologationStateBundle state)
    {
        if (state.Version != 1 || state.SavedAtUtc == default || state.Tickets is null || state.Tickets.Count > 64 || state.LoginGuards is null || state.LoginGuards.Count > 64)
            throw InvalidBundle();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (ProtectedHomologationTicketState ticket in state.Tickets)
        {
            if (ticket is null || string.IsNullOrWhiteSpace(ticket.IdentityKey) || ticket.IdentityKey.Length != 64 || ticket.IdentityKey.Any(character => !Uri.IsHexDigit(character)) ||
                !identities.Add(ticket.IdentityKey) || string.IsNullOrWhiteSpace(ticket.TicketXml) || ticket.TicketXml.Length > 256 * 1024)
                throw InvalidBundle();
        }
        identities.Clear();
        foreach (ProtectedHomologationLoginGuardState guard in state.LoginGuards)
            if (guard is null || string.IsNullOrWhiteSpace(guard.IdentityKey) || guard.IdentityKey.Length != 64 || guard.IdentityKey.Any(character => !Uri.IsHexDigit(character)) ||
                !identities.Add(guard.IdentityKey) || guard.LastAttemptUtc == default || guard.CooldownUntilUtc < guard.LastAttemptUtc.AddMinutes(10) ||
                guard.CooldownUntilUtc > guard.LastAttemptUtc.AddMinutes(12)) throw InvalidBundle();
        if (state.JournalSnapshotBase64 is { } snapshot)
        {
            byte[] bytes;
            try { bytes = Convert.FromBase64String(snapshot); }
            catch (FormatException) { throw InvalidBundle(); }
            if (bytes.Length > MaximumDecompressedLength) throw InvalidBundle();
        }
    }

    private static InvalidOperationException InvalidBundle()
        => new("Protected homologation state is malformed or exceeds its size limits.");
}

public sealed record ProtectedHomologationStateBundle(
    int Version,
    DateTimeOffset SavedAtUtc,
    List<ProtectedHomologationTicketState> Tickets,
    List<ProtectedHomologationLoginGuardState> LoginGuards,
    string? JournalSnapshotBase64)
{
    public override string ToString() => "ProtectedHomologationStateBundle { redacted }";
}

public sealed record ProtectedHomologationTicketState(string IdentityKey, string TicketXml)
{
    public override string ToString() => "ProtectedHomologationTicketState { redacted }";
}

public sealed record ProtectedHomologationLoginGuardState(string IdentityKey, DateTimeOffset LastAttemptUtc, DateTimeOffset CooldownUntilUtc)
{
    public override string ToString() => "ProtectedHomologationLoginGuardState { redacted }";
}

