# ADR-0001: UTC-normalized timestamps

Date: 2026-01-11
Status: Accepted

## Context

This library accepts timestamps from diverse sources and must behave deterministically across time zones and offsets.
Offsets are valuable for callers, but storing them in hot paths increases cost and can complicate ordering.

## Decision

All incoming timestamps are normalized to UTC at ingestion time. Bucket keys, Start/End, and LastDate are stored in UTC.
Public APIs still accept DateTimeOffset to allow callers to supply any offset they have.

## Consequences

- Deterministic ordering across time zones and offsets.
- Lower overhead for comparisons and bucket keys.
- Offsets are not preserved internally; callers that need offsets must store them separately.
