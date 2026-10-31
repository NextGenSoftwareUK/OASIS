// Fixture-only durable ownership boundary. Production uses the authenticated outbox.
export async function journaledMutation(invoke, request, record, markUnknown) {
  let prepared = false;
  try {
    const result = await invoke(request, async evidence => {
      await record({ stage: 'prepared', operation: request.operation, evidence });
      prepared = true;
    });
    await record({ stage: 'returned', operation: request.operation,
      transactionHash: result.transactionHash ?? null, tokenIds: result.tokenIds ?? null });
    return result;
  } catch (error) {
    if (prepared || error.nativeMutation) {
      markUnknown();
      try { await record({ stage: 'unresolved', operation: request.operation,
        nativeMutation: error.nativeMutation ?? null, causeType: error.constructor?.name ?? 'Error' }); }
      catch (journalError) {
        throw new AggregateError([error, journalError], 'Fixture outcome/ownership recording failed; retain prepared journal');
      }
    }
    throw error;
  }
}
