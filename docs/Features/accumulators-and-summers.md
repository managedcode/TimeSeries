# Accumulators and Summers

## Summary

Accumulators store raw events per time bucket. Summers store aggregated values per bucket. Both are optimized for
high-throughput metrics, with concurrent reads/writes and minimal allocations.

## Goals

- Provide fast ingestion of events into time buckets.
- Support aggregation strategies (sum/min/max/replace) with deterministic bucket keys.
- Keep hot paths lock-free and allocation-conscious.
- Support grouped series keyed by string for multi-tenant metrics.

## Non-goals

- Persist offsets inside buckets (timestamps are UTC-normalized).
- Provide built-in storage backends (serialization helpers only).

## Key Types

- Accumulators: Int/Float/Double accumulators with per-bucket queues.
- Summers: Int/Float/Double and generic numeric summers with per-bucket aggregation.
- Groups: String-keyed groups for accumulators and summers.

## Thread Safety

- All public types support concurrent reads and writes.
- ConcurrentDictionary and ConcurrentQueue are used for storage.
- Enumerations provide snapshot views and are safe during writes.

## Performance Notes

- MaxSamplesCount limits bucket count; 0 means unbounded.
- Summaries operate on unordered storage to avoid repeated sorts.
- Capacity trimming removes oldest buckets in a single pass.
