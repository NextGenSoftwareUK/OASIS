using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Contracts;

namespace NextGenSoftware.OASIS.STARAPI.Client;
public sealed partial class OGEngineClient
{
    private void StartWorkers()
    {
        StartAddItemWorker();
        StartUseItemWorker();
        StartQuestObjectiveWorker();
        // Generic and dedicated workers (AuthSession, Profile, Inventory, Quests) are started lazily when first used.
    }

    private void StopWorkers()
    {
        StopAddItemWorker();
        StopUseItemWorker();
        StopQuestObjectiveWorker();
        StopGenericBackgroundWorker();
        StopDedicatedWorkers();
    }

    private void StopDedicatedWorkers()
    {
        StopAuthSessionWorker();
        StopProfileWorker();
        StopInventoryWorker();
        StopQuestsWorker();
    }

    private void StartGenericBackgroundWorker()
    {
        lock (_genericBackgroundLock)
        {
            if (_genericBackgroundWorker is { IsCompleted: false })
                return;
            _genericBackgroundCts = new CancellationTokenSource();
            _genericBackgroundWorker = Task.Run(() => ProcessGenericBackgroundJobsAsync(_genericBackgroundCts.Token));
        }
    }

    private void StopGenericBackgroundWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_genericBackgroundLock)
        {
            cts = _genericBackgroundCts;
            worker = _genericBackgroundWorker;
            _genericBackgroundCts = null;
            _genericBackgroundWorker = null;
        }
        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _genericBackgroundSignal.Release();
                worker?.GetAwaiter().GetResult();
            }
            catch { }
            finally { cts.Dispose(); }
        }
        while (_genericBackgroundQueue.TryDequeue(out _)) { }
    }

    private void StartAuthSessionWorker()
    {
        lock (_authSessionLock)
        {
            if (_authSessionWorker is { IsCompleted: false }) return;
            _authSessionCts = new CancellationTokenSource();
            _authSessionWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_authSessionQueue, _authSessionSignal, _authSessionCts.Token));
        }
    }

    private void StopAuthSessionWorker()
    {
        StopDedicatedWorker(_authSessionLock, ref _authSessionCts, ref _authSessionWorker, _authSessionSignal, _authSessionQueue);
    }

    private void StartProfileWorker()
    {
        lock (_profileLock)
        {
            if (_profileWorker is { IsCompleted: false }) return;
            _profileCts = new CancellationTokenSource();
            _profileWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_profileQueue, _profileSignal, _profileCts.Token));
        }
    }

    private void StopProfileWorker()
    {
        StopDedicatedWorker(_profileLock, ref _profileCts, ref _profileWorker, _profileSignal, _profileQueue);
    }

    private void StartInventoryWorker()
    {
        lock (_inventoryLock)
        {
            if (_inventoryWorker is { IsCompleted: false }) return;
            _inventoryCts = new CancellationTokenSource();
            _inventoryWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_inventoryQueue, _inventorySignal, _inventoryCts.Token));
        }
    }

    private void StopInventoryWorker()
    {
        StopDedicatedWorker(_inventoryLock, ref _inventoryCts, ref _inventoryWorker, _inventorySignal, _inventoryQueue);
    }

    private void StartQuestsWorker()
    {
        lock (_questsLock)
        {
            if (_questsWorker is { IsCompleted: false }) return;
            _questsCts = new CancellationTokenSource();
            _questsWorker = Task.Run(() => ProcessDedicatedWorkerAsync(_questsQueue, _questsSignal, _questsCts.Token));
        }
    }

    private void StopQuestsWorker()
    {
        /* Quest progress runs on this queue; StopDedicatedWorker used to cancel then discard remaining jobs,
         * so exit/reload could drop POST /quests/.../progress that had not run yet (or abort in-flight).
         * Give the worker time to drain, a short grace for the last HTTP, then run any leftovers synchronously. */
        const int spinMs = 10;
        var spinDeadline = DateTime.UtcNow.AddSeconds(12);
        var sawQueuedWork = !_questsQueue.IsEmpty;
        while (DateTime.UtcNow < spinDeadline && !_questsQueue.IsEmpty)
        {
            sawQueuedWork = true;
            try { _questsSignal.Release(1); }
            catch { /* Semaphore disposed or at capacity — stop spinning */ break; }
            Thread.Sleep(spinMs);
        }

        /* Brief grace so an in-flight progress POST can finish; longer if we were draining the queue. */
        Thread.Sleep(sawQueuedWork ? TimeSpan.FromSeconds(2) : TimeSpan.FromMilliseconds(500));

        CancellationTokenSource? c;
        Task? w;
        lock (_questsLock)
        {
            c = _questsCts;
            w = _questsWorker;
            _questsCts = null;
            _questsWorker = null;
        }

        if (c is not null)
        {
            try
            {
                c.Cancel();
                try { _questsSignal.Release(1); } catch { /* ignore */ }
                w?.GetAwaiter().GetResult();
            }
            catch { /* join */ }
            finally { c.Dispose(); }
        }

        var drainSw = Stopwatch.StartNew();
        while (drainSw.Elapsed < TimeSpan.FromSeconds(10) && _questsQueue.TryDequeue(out var job))
        {
            try
            {
                job(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                OGEngineExports.StarApiLogFileOnly($"[Quest] Shutdown drain job failed: {ex.Message}");
            }
        }

        if (!_questsQueue.IsEmpty)
        {
            var dropped = 0;
            while (_questsQueue.TryDequeue(out _)) dropped++;
            OGEngineExports.StarApiLogFileOnly($"[Quest] Shutdown: {dropped} queued job(s) not drained after budget — progress may not persist.");
        }
    }

    private static async Task ProcessDedicatedWorkerAsync(ConcurrentQueue<Func<CancellationToken, Task>> queue, SemaphoreSlim signal, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            while (queue.TryDequeue(out var job))
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    await job(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch { /* TCS already set */ }
            }
        }
    }

    private void StopDedicatedWorker(object lockObj, ref CancellationTokenSource? cts, ref Task? worker, SemaphoreSlim signal, ConcurrentQueue<Func<CancellationToken, Task>> queue)
    {
        CancellationTokenSource? c;
        Task? w;
        lock (lockObj)
        {
            c = cts; w = worker;
            cts = null; worker = null;
        }
        if (c is not null)
        {
            try
            {
                c.Cancel();
                signal.Release();
                w?.GetAwaiter().GetResult();
            }
            catch { }
            finally { c.Dispose(); }
        }
        while (queue.TryDequeue(out _)) { }
    }

    /// <summary>Run an operation on a dedicated worker (AuthSession, Profile, Inventory, Quests) so it doesn't block the generic worker or other domains.</summary>
    private Task<OASISResult<T>> RunOnWorkerAsync<T>(DedicatedWorker workerType, Func<CancellationToken, Task<OASISResult<T>>> operation, CancellationToken cancellationToken)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<T>("Client is not initialized.", StarApiResultCode.NotInitialized));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<OASISResult<T>>(cancellationToken);

        var tcs = new TaskCompletionSource<OASISResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        var run = async (CancellationToken workerCt) =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(workerCt, cancellationToken);
            try
            {
                var result = await operation(linked.Token).ConfigureAwait(false);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException) { tcs.TrySetCanceled(); }
            catch (Exception ex) { tcs.TrySetResult(Fail<T>(ex.Message, StarApiResultCode.Network, ex)); }
        };

        switch (workerType)
        {
            case DedicatedWorker.AuthSession:
                _authSessionQueue.Enqueue(run);
                _authSessionSignal.Release();
                StartAuthSessionWorker();
                break;
            case DedicatedWorker.Profile:
                _profileQueue.Enqueue(run);
                _profileSignal.Release();
                StartProfileWorker();
                break;
            case DedicatedWorker.Inventory:
                _inventoryQueue.Enqueue(run);
                _inventorySignal.Release();
                StartInventoryWorker();
                break;
            case DedicatedWorker.Quests:
                _questsQueue.Enqueue(run);
                _questsSignal.Release();
                StartQuestsWorker();
                break;
        }
        return tcs.Task;
    }

    private async Task ProcessGenericBackgroundJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _genericBackgroundSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            while (_genericBackgroundQueue.TryDequeue(out var job))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    await job(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    /* Job already set result/exception on its TCS; continue to next job. */
                }
            }
        }
    }

    /// <summary>Run an async operation on the generic background worker so the caller's thread (e.g. UI/game) never blocks. Returns a Task that completes when the operation finishes.</summary>
    private Task<OASISResult<T>> RunOnBackgroundAsync<T>(Func<CancellationToken, Task<OASISResult<T>>> operation, CancellationToken cancellationToken)
    {
        if (!IsInitialized())
            return Task.FromResult(FailAndCallback<T>("Client is not initialized.", StarApiResultCode.NotInitialized));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<OASISResult<T>>(cancellationToken);

        var tcs = new TaskCompletionSource<OASISResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        var run = async (CancellationToken workerCt) =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(workerCt, cancellationToken);
            try
            {
                var result = await operation(linked.Token).ConfigureAwait(false);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(Fail<T>(ex.Message, StarApiResultCode.Network, ex));
            }
        };

        _genericBackgroundQueue.Enqueue(run);
        _genericBackgroundSignal.Release();
        StartGenericBackgroundWorker();
        return tcs.Task;
    }

    private void StartAddItemWorker()
    {
        lock (_jobLock)
        {
            if (_jobWorker is { IsCompleted: false })
                return;

            _jobCts = new CancellationTokenSource();
            _jobWorker = Task.Run(() => ProcessAddItemJobsAsync(_jobCts.Token));
        }
    }

    private void StopAddItemWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _jobCts;
            worker = _jobWorker;
            _jobCts = null;
            _jobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _addItemSignal.Release();
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingAddItemJobs.TryDequeue(out var pending))
            pending.Completion?.TrySetResult(Fail<StarItem>("Add-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartUseItemWorker()
    {
        lock (_jobLock)
        {
            if (_useItemJobWorker is not null && !_useItemJobWorker.IsCompleted)
                return;

            _useItemJobCts = new CancellationTokenSource();
            _useItemJobWorker = Task.Run(() => ProcessUseItemJobsAsync(_useItemJobCts.Token));
        }
    }

    private void StopUseItemWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _useItemJobCts;
            worker = _useItemJobWorker;
            _useItemJobCts = null;
            _useItemJobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _useItemSignal.Release();
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingUseItemJobs.TryDequeue(out var pending))
            pending.Completion?.TrySetResult(Fail<bool>("Use-item queue stopped.", StarApiResultCode.NotInitialized));
    }

    private void StartQuestObjectiveWorker()
    {
        lock (_jobLock)
        {
            if (_questObjectiveJobWorker is not null && !_questObjectiveJobWorker.IsCompleted)
                return;

            _questObjectiveJobCts = new CancellationTokenSource();
            _questObjectiveJobWorker = Task.Run(() => ProcessQuestObjectiveJobsAsync(_questObjectiveJobCts.Token));
        }
    }

    private void StopQuestObjectiveWorker()
    {
        CancellationTokenSource? cts;
        Task? worker;
        lock (_jobLock)
        {
            cts = _questObjectiveJobCts;
            worker = _questObjectiveJobWorker;
            _questObjectiveJobCts = null;
            _questObjectiveJobWorker = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
                _questObjectiveSignal.Release();
                if (worker is not null)
                    worker.GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        while (_pendingQuestObjectiveJobs.TryDequeue(out var pending))
            pending.Completion.TrySetResult(Fail<bool>("Quest objective queue stopped.", StarApiResultCode.NotInitialized));
    }

    /// <summary>Background worker: flush local pending to API (one add_item per type), then invalidate cache. Games only call EnqueueAddItemJobOnly or EnqueuePickupWithMintJobOnly; this does the heavy lifting.</summary>
    private async Task ProcessAddItemJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _addItemSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            /* Flush pending XP (queued by ogengine_queue_add_xp or monster kill jobs). */
            var pendingXp = Interlocked.Exchange(ref _pendingXp, 0);
            if (pendingXp > 0)
            {
                var addXpResult = await AddXpAsync(pendingXp, cancellationToken).ConfigureAwait(false);
                if (addXpResult.IsError)
                    OGEngineExports.SetLastBackgroundError($"STAR: Add XP failed: {addXpResult.Message}");
            }

            /* Process monster kill jobs: add XP and optionally mint + add item. Flush XP immediately after so it shows up as soon as you kill. */
            while (_pendingMonsterKill.TryDequeue(out var monsterJob))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                OGEngineExports.StarApiLog($"Monster kill processing: {monsterJob.DisplayName} {monsterJob.Xp} XP doMint={monsterJob.DoMint}");
                Interlocked.Add(ref _pendingXp, monsterJob.Xp);
                if (!monsterJob.DoMint)
                    continue;
                var gameSource = string.IsNullOrWhiteSpace(monsterJob.GameSource) ? "ODOOM" : monsterJob.GameSource;
                var desc = $"Monster defeated in {gameSource}: {monsterJob.DisplayName}";
                OGEngineExports.StarApiLog($"Monster kill: minting NFT for {monsterJob.DisplayName}");
                var mintResult = await CreateMonsterNftAsync(monsterJob.EngineName, desc, gameSource, "{}", monsterJob.Provider, cancellationToken).ConfigureAwait(false);
                if (mintResult.IsError || string.IsNullOrWhiteSpace(mintResult.Result.NftId))
                {
                    OGEngineExports.StarApiLog($"Monster kill: NFT mint failed for '{monsterJob.DisplayName}': {mintResult.Message}");
                    OGEngineExports.SetLastBackgroundError($"STAR: Monster NFT mint failed for '{monsterJob.DisplayName}': {mintResult.Message}");
                    continue;
                }
                OGEngineExports.StarApiLog($"Monster kill: NFT minted for {monsterJob.DisplayName}, adding to inventory");
                /* Store item name with game source so OQUAKE and ODOOM kills are separate (e.g. "Dog (OQUAKE)" vs "Dog (ODOOM)"). Add [BOSS] for boss monsters only. */
                var baseName = monsterJob.IsBoss ? "[BOSS] " + monsterJob.DisplayName : monsterJob.DisplayName;
                var itemName = $"{baseName} ({gameSource})";
                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var addResult = await AddItemCoreAsync(itemName, desc, gameSource, "Monster", mintResult.Result.NftId, 1, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        OGEngineExports.SetLastBackgroundError($"STAR: Add monster item failed for '{itemName}': {addResult.Message}");
                    else
                    {
                        lock (_lastMintLock)
                        {
                            _lastMintItemName = itemName;
                            _lastMintNftId = mintResult.Result.NftId;
                            _lastMintHash = mintResult.Result.Hash;
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Flush XP from monster kills (and any other pending) so HUD updates as soon as you kill, not on next worker wake. */
            var monsterXp = Interlocked.Exchange(ref _pendingXp, 0);
            if (monsterXp > 0)
            {
                OGEngineExports.StarApiLog($"Monster kill: sending AddXpAsync({monsterXp}) to API");
                var addXpResult = await AddXpAsync(monsterXp, cancellationToken).ConfigureAwait(false);
                if (addXpResult.IsError)
                {
                    OGEngineExports.StarApiLog($"Monster kill: Add XP failed: {addXpResult.Message}");
                    OGEngineExports.SetLastBackgroundError($"STAR: Add XP failed: {addXpResult.Message}");
                }
                else
                    OGEngineExports.StarApiLog($"Monster kill: Add XP succeeded, new total={addXpResult.Result}");
            }

            // Process pickup-with-mint jobs first (mint then add_item; all in C# background).
            while (_pendingPickupWithMint.TryDequeue(out var pickupJob))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                string? nftId = null;
                if (pickupJob.DoMint)
                {
                    var mintResult = await MintInventoryItemNftAsync(
                        pickupJob.ItemName,
                        pickupJob.Description,
                        pickupJob.GameSource,
                        pickupJob.ItemType,
                        pickupJob.Provider,
                        pickupJob.SendToAddressAfterMinting,
                        cancellationToken).ConfigureAwait(false);
                    if (!mintResult.IsError && mintResult.Result.NftId is { } id)
                    {
                        nftId = id;
                        var hash = mintResult.Result.Hash;
                        lock (_lastMintLock)
                        {
                            _lastMintItemName = pickupJob.ItemName;
                            _lastMintNftId = id;
                            _lastMintHash = string.IsNullOrWhiteSpace(hash) ? null : hash;
                        }
                        /* So overlay shows [NFT] before add completes: set NftId on pending entry. */
                        lock (_localPendingLock)
                        {
                            if (_localPending.TryGetValue(pickupJob.ItemName, out var pending))
                                pending.NftId = id;
                        }
                    }
                    else if (mintResult.IsError)
                    {
                        OGEngineExports.StarApiLog($"Mint failed for '{pickupJob.ItemName}': {mintResult.Message}");
                        OGEngineExports.SetLastBackgroundError($"STAR: Mint failed for '{pickupJob.ItemName}': {mintResult.Message}");
                    }
                }
                Interlocked.Increment(ref _activeAddItemJobs);
                try
                {
                    var addResult = await AddItemCoreAsync(pickupJob.ItemName, pickupJob.Description, pickupJob.GameSource, pickupJob.ItemType, nftId, pickupJob.Quantity, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        OGEngineExports.SetLastBackgroundError($"STAR: Add item failed for '{pickupJob.ItemName}': {addResult.Message}");
                    else
                        DeductLocalPending(pickupJob.ItemName, pickupJob.Quantity);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Do not invalidate cache here: AddItemCoreAsync already updates _cachedInventory when add succeeds. Invalidating caused a refetch that could return stale data (keys vanished in overlay). */

            Dictionary<string, LocalPendingEntry> snapshot;
            lock (_localPendingLock)
            {
                if (_localPending.Count == 0)
                    continue;
                snapshot = new Dictionary<string, LocalPendingEntry>(_localPending, StringComparer.OrdinalIgnoreCase);
                _localPending.Clear();
            }

            /* Ensure FlushAddItemJobsAsync does not return until all items are processed (avoids race where HasItemAsync runs with cache not yet updated). */
            var snapshotCount = snapshot.Count;
            Interlocked.Add(ref _activeAddItemJobs, snapshotCount);

            if (AddItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(AddItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    lock (_localPendingLock)
                    {
                        foreach (var kv in snapshot)
                            _localPending[kv.Key] = kv.Value;
                    }
                    break;
                }
            }

            foreach (var kv in snapshot)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    lock (_localPendingLock)
                    {
                        if (_localPending.TryGetValue(kv.Key, out var existing))
                            existing.Quantity += kv.Value.Quantity;
                        else
                            _localPending[kv.Key] = kv.Value;
                    }
                    Interlocked.Decrement(ref _activeAddItemJobs);
                    continue;
                }
                var entry = kv.Value;
                try
                {
                    var addResult = await AddItemCoreAsync(entry.Name, entry.Description, entry.GameSource, entry.ItemType, null, entry.Quantity, true, cancellationToken).ConfigureAwait(false);
                    if (addResult.IsError)
                        OGEngineExports.SetLastBackgroundError($"STAR: Add item failed for '{entry.Name}': {addResult.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref _activeAddItemJobs);
                }
            }

            /* Do not invalidate cache: AddItemCoreAsync already updated _cachedInventory for each added item. */
        }
    }

    private async Task ProcessUseItemJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingUseItemJob>(Math.Max(1, UseItemBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _useItemSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingUseItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, UseItemBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (UseItemBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(UseItemBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingUseItemJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, UseItemBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion?.TrySetResult(Fail<bool>("Queued use-item job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeUseItemJobs);
                try
                {
                    var result = await UseItemCoreAsync(job.ItemName, job.Context, job.Quantity, job.CancellationToken).ConfigureAwait(false);
                    job.Completion?.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeUseItemJobs);
                }
            }
        }
    }

    private async Task ProcessQuestObjectiveJobsAsync(CancellationToken cancellationToken)
    {
        var batch = new List<PendingQuestObjectiveJob>(Math.Max(1, QuestObjectiveBatchSize));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _questObjectiveSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            while (_pendingQuestObjectiveJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, QuestObjectiveBatchSize))
                batch.Add(pending);

            if (batch.Count == 0)
                continue;

            if (QuestObjectiveBatchWindow > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(QuestObjectiveBatchWindow, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                while (_pendingQuestObjectiveJobs.TryDequeue(out var pending) && batch.Count < Math.Max(1, QuestObjectiveBatchSize))
                    batch.Add(pending);
            }

            foreach (var job in batch)
            {
                if (job.CancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    job.Completion.TrySetResult(Fail<bool>("Queued quest objective job was cancelled.", StarApiResultCode.Network));
                    continue;
                }

                Interlocked.Increment(ref _activeQuestObjectiveJobs);
                try
                {
                    var result = await CompleteQuestObjectiveCoreAsync(job.QuestId, job.ObjectiveId, job.GameSource, job.CancellationToken).ConfigureAwait(false);
                    job.Completion.TrySetResult(result);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeQuestObjectiveJobs);
                }
            }
        }
    }
}
