---
trigger: manual
---

# Skill: Photon Fusion Networking Architecture Reviewer

## Purpose

Review and improve the multiplayer architecture of a Photon Fusion project.

Focus on:

- Server-authoritative design
- State ownership
- Synchronization correctness
- RPC usage
- Security
- Scalability

---

## Architecture Rules

The project must follow a server-authoritative model.

Server/Host:

- Owns authoritative game state
- Validates gameplay actions
- Replicates state changes

Clients:

- Send input
- Request actions
- Display replicated state

Clients must never directly modify authoritative state.

---

## Authority Review

Inspect all NetworkBehaviours.

Verify:

- StateAuthority usage
- InputAuthority usage
- Ownership transfer logic

Flag:

- Invalid authority checks
- Client-side state modifications
- Missing authority validation

---

## Input System Review

Player control must follow:

Client Input
→ Prediction
→ Server Validation
→ Replication

Verify:

- NetworkInputData usage
- Input collection implementation
- Prediction support

Flag:

- Movement RPCs
- Rotation RPCs
- Client-authoritative movement

---

## Networked State Review

Review all Networked properties.

For each property determine:

- Why it is synchronized
- Who owns it
- Whether synchronization is necessary

Recommend removal of unnecessary replicated state.

---

## RPC Review

Inspect all RPC methods.

Classify each RPC as:

- Valid gameplay event
- Potential state replication misuse
- Bandwidth risk

RPCs should be used only for:

- Interactions
- One-time actions
- Gameplay events

Flag:

- Movement RPCs
- Continuous update RPCs
- Tick-based RPC usage

---

## Gameplay Object Review

Review:

- NetworkObject usage
- NetworkBehaviour design
- Object ownership

Verify:

- Proper spawning
- Proper despawning
- Correct authority assignment

Prefer IDs over object references whenever possible.

---

## Security Review

Identify:

- Client-authoritative logic
- Trust-based RPCs
- State manipulation risks
- Validation bypasses

Recommend authoritative alternatives.

---

## Fusion Best Practices

Verify:

- StateAuthority
- InputAuthority
- Prediction
- Interpolation
- ChangeDetector

Flag violations.

---

## Deliverables

Provide:

### Findings

List all detected networking issues.

### Risk Assessment

For each issue:

- Severity
- Impact
- Explanation

### Refactoring Plan

Prioritized fixes:

1. Critical
2. High
3. Medium
4. Low