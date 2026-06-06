---
trigger: manual
---

# Skill: Photon Fusion Performance & Bandwidth Optimizer

## Purpose

Analyze and optimize the network performance of a Photon Fusion multiplayer project.

Focus on:

- Bandwidth reduction
- Replication efficiency
- Tick optimization
- Snapshot optimization
- Scalability

---

## Tick Analysis

Review:

- Runner.TickRate
- Simulation frequency
- Snapshot frequency
- Send frequency

Determine:

- Current network cost
- Recommended configuration

---

## Replication Analysis

Inspect all replicated state.

Measure:

- Number of Networked properties
- Update frequency
- Data size

Identify:

- Over-replicated data
- Redundant synchronization
- Large replicated structures

Recommend optimizations.

---

## Bandwidth Review

Estimate:

- Bandwidth per player
- Bandwidth per tick
- Worst-case bandwidth usage

Identify:

- Top bandwidth consumers
- Excessive state updates
- High-frequency replication

---

## RPC Performance Review

Analyze:

- RPC count
- RPC frequency
- Payload size

Flag:

- Frequent RPC usage
- Per-tick RPCs
- Replication that should use state sync

---

## Network Collection Review

Inspect:

- NetworkArray
- NetworkDictionary
- NetworkLinkedList

Check:

- Collection size
- Modification frequency
- Replication cost

Suggest alternatives when appropriate.

---

## Spawn/Despawn Analysis

Review all NetworkObjects.

Identify:

- Excessive spawning
- Excessive despawning
- Pooling opportunities

Recommend object pooling where beneficial.

---

## Visibility Optimization

Review object relevance.

Identify:

- Objects replicated to all players unnecessarily
- Visibility optimization opportunities
- Interest management opportunities

Reduce unnecessary replication.

---

## State Compression Review

Verify:

- Delta compression effectiveness
- State change frequency
- Snapshot efficiency

Recommend methods to reduce replicated data size.

---

## Scalability Review

Estimate performance for:

- 2 players
- 4 players
- 8 players
- 16 players

Identify bottlenecks.

---

## Deliverables

Provide:

### Network Metrics

- Estimated bandwidth usage
- Active NetworkObject count
- Networked field count
- RPC count
- Tick configuration

### Bottleneck Report

List:

- Major bandwidth consumers
- Major replication costs
- Major scalability risks

### Optimization Plan

Prioritized improvements with expected impact.