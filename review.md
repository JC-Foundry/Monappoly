# Ultimate Monopoly code review — 2026-06-20

## Scope
- Source: `Ultimate-Monopoly-master (2).zip`.
- Reviewed statically: ASP.NET app, game engine, SignalR hubs, cache/executor, cards, prompts, turn flow, transactions, board skins, stats, tests, docs/session notes.
- Build/test status: **not run**. Sandbox has no `.NET SDK` (`dotnet: command not found`). Findings below are static-analysis findings only.
- Tests present: `MP.GameEngine.Tests` only, 6 test files, 189 `[Fact]`/`[Theory]` entries. No web/integration/UI test coverage found.

## Architecture map
- Setup: `GameSetupService.TryStartGame` -> `GameEngineSetupService.SetupGameCache` -> `SnapshotService.CreateSnapshotAsync` -> `GameCacheService.PopulateGame` -> `GameService.EnqueueTurn`.
- Runtime: `GamePlayHub` optimistic checks -> `GameExecutor` per-game pump -> scoped `GameEngineFactory` -> engine service -> `TurnStateProvider` commits snapshots/events.
- Prompts: engine opens `PromptProvider.RequestAsync` -> clients submit via `GamePlayHub.SubmitPrompt` -> direct TCS resolution, bypassing pump by design.
- Cards: `CardCacheService` -> `CardImportService` -> decks on game cache -> `CardService.DrawCard/PlayCard` -> action services -> event receipts -> stats.
- Completion: engine/game service -> `GameCompletionService.ConcludeGame` -> DB outcome/user counters -> Hangfire `StatisticsJob`.

## Critical / high findings

### H-01 — Game completion clears live runtime before DB commit
- **Where:** `UltimateMonopoly/Services/GameEngine/GameCompletionService.cs:70-72`, commit starts at `:135`.
- **Evidence:** `ClearGameRuntime(gameCache.GameId)` invalidates cache/stops pump before `BeginTransactionAsync` and before `SaveChangesAsync/CommitTransactionAsync`.
- **Impact:** if DB update fails, the in-memory game is destroyed while DB still says in-play. Next access can rehydrate from old snapshot, causing a zombie/replayed game or inconsistent client state.
- **Fix:** move `ClearGameRuntime` after successful commit. Keep notify/stats after commit. If called on pump, keep stop fire-and-forget, but only after durable finish.

### H-02 — Board skin cache invalidation is effectively blocked for normal users
- **Where:** `UltimateMonopoly/Services/Cache/BoardCacheService.cs:53-59`; callers pass current/shared user IDs in `BoardSkinService.cs:135,147,214,254,266,282` and `BoardSkinShareService.cs:135,160`.
- **Evidence:** `Invalidate(userId)` returns immediately when `userId` is non-empty and caller is not SystemAdmin.
- **Impact:** normal users creating/editing/deleting/sharing board skins do not clear their cached board list. UI/game setup can show stale board names/spaces/shares for up to 6h.
- **Fix:** allow invalidating own user cache; reserve admin check only for invalidating another user's cache. For share updates, invalidate affected users from a system/admin path or explicit trusted method.

### H-03 — `MovementStatsService` can throw when a player has no counted landings
- **Where:** `MP.GameEngine/Services/Statistics/MovementStatsService.cs:126`.
- **Evidence:** `record.MostLandedOnBoardIndex = landOnIndexes.MaxBy(kv => kv.Value).Key;` with no empty guard.
- **Impact:** a finished/cancel-edge/drawn/early-bankrupt/no-move player can make stats projection fail for the whole game block; `StatisticsJob` catches and logs, then no stats are written for that game.
- **Fix:** if empty, set `MostLandedOnBoardIndex = null` if model permits, or a sentinel/default (`Go`) with a `0` count. Add a regression test.

### H-04 — Card import silently ignores missing/empty card files
- **Where:** `UltimateMonopoly/Services/Imports/CardImportService.cs:51-61`.
- **Evidence:** missing file and empty/null deserialisation both `continue`.
- **Impact:** a bad deploy/config can start with missing decks and no hard failure. Gameplay then fails later or behaves like cards do not exist.
- **Fix:** required configured card files should fail fast with file name/path. Only allow optional decks if explicitly configured as optional.

## Medium findings

### M-01 — `CardOptionPrompt` validation accepts any key for play-card choices
- **Where:** `MP.GameEngine/Services/Framework/PromptValidator.cs:140-153`.
- **Evidence:** `return prompt.PlayCardChoice || prompt.Options.Any(o => o.Key == r.SelectedKey);`.
- **Impact:** when `PlayCardChoice == true`, any arbitrary `SelectedKey` is valid. Normal hand-play usually no-ops later; immunity prompts are worse because any non-empty key accepts the one server-found immunity card.
- **Related:** `CardImmunityService.cs:44-53` plays `immunityCard` if `SelectedKey` is non-empty; it does not compare the selected key to `immunityCard.CardId`.
- **Fix:** for `PlayCardChoice`, allow empty decline OR a key from `Options`. In immunity service, also require `response.SelectedKey == immunityCard.CardId`.

### M-02 — Game cache hydration is not single-flight
- **Where:** `UltimateMonopoly/Services/Cache/GameCacheService.cs:55-60`, `:86-110`.
- **Evidence:** manual `TryGetValue` then `HydrateAsync`; no per-game lock/lazy task.
- **Impact:** simultaneous cache misses can create multiple `GameCacheModel` instances for one game. Pump may mutate one instance while hub prompt/state reads hit another, causing rejected prompt submissions or stale optimistic checks after expiry/fault recovery.
- **Fix:** use per-game async lock or `Lazy<Task<GameCacheModel?>>` single-flight around hydrate. Keep one in-memory working copy per game.

### M-03 — Deals are documented as any-player-at-boundary, but code restricts to current player
- **Where:** docs/comment in `UltimateMonopoly/Hubs/GamePlayHub.cs:197-203`; gate in `MP.GameEngine/Services/Framework/TurnStateProvider.cs:91-95`.
- **Evidence:** hub says proposer need not be current player; `CanDeal` requires `IsCurrentPlayer(playerId)`.
- **Impact:** non-current players cannot propose deals at a turn boundary, despite the public contract/comment saying they can.
- **Fix:** decide rule. If any active player may propose at boundaries, remove `IsCurrentPlayer(playerId)` and add player-active/not-bankrupt checks. If only current player may propose, update comments/UI.

### M-04 — `GameExecutor` queue is unbounded
- **Where:** `UltimateMonopoly/Services/GameEngine/GameExecutor.cs:208-209`, enqueue at `:236-240`.
- **Evidence:** `Channel.CreateUnbounded<GameWorkItem>`.
- **Impact:** repeated hub commands while a prompt is parked can grow memory/queue indefinitely per game. Re-checks will reject stale work later, but only after queued work is drained.
- **Fix:** bounded channel or command coalescing/deduplication for idempotent actions; return false/backpressure when queue is saturated.

### M-05 — Statistics are write-once and cannot self-correct after stats logic fixes
- **Where:** `UltimateMonopoly/Services/Statistics/StatisticsJob.cs:66-72`, `:123-126`.
- **Evidence:** skips a game if all player stat rows already exist; only fills missing rows.
- **Impact:** bugs fixed in stat services do not update existing `PlayerGameStat` rows. Card-stat migration now exists, but future corrections need manual delete/backfill.
- **Fix:** add versioned stats schema or explicit recompute mode per game/all games.

### M-06 — Jail exit stats misclassify card exits as dice exits
- **Where:** `MP.GameEngine/Services/Statistics/JailStatsService.cs:30-42`.
- **Evidence:** `const int leftByCard = 0` TODO.
- **Impact:** `TimesLeftJailByPlayingCard` is always 0. Card exits inflate `TimesLeftJailByDice`.
- **Fix:** detect `CardPlayedReceipt` with jail-exit action/trigger in same turn/player, or emit a dedicated `PlayerLeftJailReceipt` with reason.

### M-07 — Card dice rolls are not counted in movement stats
- **Where:** `MP.GameEngine/Services/SubSystems/DiceService.cs:64-72`; `MP.GameEngine/Services/Statistics/MovementStatsService.cs:40-42`.
- **Evidence:** card rolls emit no `DiceRollReceipt`; `cardRolls += 0` TODO.
- **Impact:** `TotalCardRolls` always 0 even though dice-off/multiplier cards now exist.
- **Fix:** emit a distinct card-roll receipt or extend `DiceRollReceipt` with safe metadata that cannot pollute turn-roll/dice-number stats.

### M-08 — Card persisted IDs are order-dependent
- **Where:** `UltimateMonopoly/Services/Imports/CardImportService.cs:50-66`, lookup at `:97-105`.
- **Evidence:** `UniqueText = rawText + [[globalIndex]]`; global index increments by import order across files.
- **Impact:** inserting/reordering cards changes IDs for unchanged cards after the insertion point, breaking continuity in stats/saved references.
- **Fix:** use explicit JSON IDs, or stable key `{deck,type,rawText,occurrenceWithinSameRawText}`. Avoid global order as identity.

### M-09 — Card model mismatch exceptions lack file/card context
- **Where:** `CardImportService.cs:116-132`, `:149-150`.
- **Evidence:** throws `Group count mismatch`, `Action count mismatch`, `Condition count mismatch` only.
- **Impact:** when card JSON changes and persisted IDs mismatch, the startup/import failure is hard to diagnose.
- **Fix:** include card text/card id/file name/group index/action index in exception messages.

### M-10 — `CardCacheService.GetCard` nullable contract is false
- **Where:** `UltimateMonopoly/Services/Cache/CardCacheService.cs:32-38`.
- **Evidence:** method returns `Task<CardModel?>` but throws `InvalidOperationException("Failed to get card")` when not found.
- **Impact:** callers cannot rely on null for unknown card ID; API contract lies.
- **Fix:** either return null or change signature to non-null and name it `GetRequiredCard`.

## Low / polish findings

### L-01 — Program rate-limit comment and value disagree
- **Where:** `UltimateMonopoly/Program.cs:69-76`.
- **Evidence:** comment says 10 req/min; `PermitLimit = 20`.
- **Impact:** security expectation/documentation drift.
- **Fix:** update comment or value.

### L-02 — Host panel comments are stale
- **Where:** `_HostPanel.cshtml:8-10`, `host-panel.js:8-9`.
- **Evidence:** comments say Force Refresh hub method is TODO; `GamePlayHub.ForceRefresh` exists and JS wires it.
- **Impact:** future devs may chase non-existent TODOs.
- **Fix:** update comments.

### L-03 — Setup creation still has SVG QR path
- **Where:** `UltimateMonopoly/Services/Games/GameSetupService.cs` around `TryCreateNewGame` QR result.
- **Evidence:** prior notes said setup-page QR moved to base64 PNG, but this service path still returns SVG format.
- **Impact:** probably harmless if this path is legacy; if used, QR behavior differs by page.
- **Fix:** confirm caller. Standardise QR payload format or mark legacy.

### L-04 — Board/stat display always uses default board
- **Where:** `UltimateMonopoly/Services/Statistics/StatisticsJob.cs:111-118`.
- **Evidence:** stats projection always loads `GetDefaultBoard()`.
- **Impact:** custom board games may show default-board property names/sets in stat-derived displays. Index-based totals remain mostly valid.
- **Fix:** load the game's actual board skin when stats need names/grouping; keep default only if stats are intentionally canonical.

### L-05 — `ShortfallAmount`/`AmountOwed` computed properties rely on caller invariant
- **Where:** `PlayerBankruptedReceipt.ShortfallAmount`; `ShortfallPrompt.AmountOwed`.
- **Evidence:** unsigned subtraction assumes `Cost > PlayerBalance`.
- **Impact:** if constructed outside strict shortfall path, values can underflow.
- **Fix:** clamp with `Math.Max(0, owed - balance)` or use signed backing calculation.

## Positive findings / things that are now solid
- Per-game single-writer executor is the right shape for this app. It prevents multiple concurrent mutations to the working game model.
- Prompt path is correctly out-of-band from the pump, avoiding the classic deadlock where a prompt response queues behind the parked command waiting for that same response.
- Dice trigger cards are wired in current code: `DiceService.RollTurnDice` calls `OnSnakeEyes`, `OnRollDouble`, `OnRollTriple`, and `OnOtherRollsTriple` before emitting `DiceRollReceipt`.
- Card stats migration now appears present (`20260619125257_CardStats.*`); older session-note debt is resolved.
- Card money actions route player-to-player collections from the payer POV for `EachPlayer`, `TriggerPlayer`, and `DiceOffPlayer`, so counterparty shortfalls are handled correctly in those paths.
- Transaction service keeps `SaveChanges` out of mid-turn service calls; commit-at-boundary design is correct for working-copy integrity.
- Game cancellation tears down runtime after DB update; completion should copy that ordering.

## Recommended fix order
1. **Before release:** H-01, H-02, H-03, H-04, M-01.
2. **Next pass:** M-02, M-03, M-04.
3. **Stats polish:** M-05, M-06, M-07, L-04.
4. **Maintainability:** M-08, M-09, M-10, L-01, L-02, L-03, L-05.

## Tests to add
- Completion failure test: simulate DB save failure; runtime must remain available until commit succeeds.
- Board skin edit/create/share invalidates current user's cached board list.
- Movement stats with no landings does not throw.
- CardOptionPrompt play-card validation: empty allowed, valid option allowed, invalid key rejected.
- Immunity prompt only plays when selected key equals the immunity card ID.
- Concurrent `GameCacheService.GetGame` cache-miss requests return the same instance.
- Non-current deal proposer behavior test matching final rule decision.
- Card import missing file fails loudly.
- Jail card exit stats and card dice-roll stats once receipt model is decided.


---

---

Append onto review the following bugs: 1) Prompts that require a count of greater than the number of options (require 2, have only 1 option!) lock
the game up (cant sumbit prompt, cos expects 2 selections but can only select 1). 2) Cards dont get played IN jail (this needs a look at; cos some
DO need to be played in jail - literal trigger!). 3) Rework of purging: you will now be able to purge the same property AGAIN. 4)
NormaliseProperties is NOT run for multiple cards, and in some edge cases - can cause game errors (build on set expects 'SET', but if not normalised
rents, these props are at 'SINGLE' rent level). 5) 0 houses on a set that is swapped and purged, are marked as purged (but there was nothjing to
purge). 6) Free hotel card (tax card) doesnt work; should GIVE the player a hotel from the pool (if available) - likely needs counter on player
model. 7) Some FP cards (like each player hands in a property) allow the landing player to hand in one of those properties (cos it transfers the
properties to player before FP method resolves). 8) The player turn orchestator needs some real work - along with dice rolls because atm there are a
few quirky bugs with upgrade/downgrade dice, steal triple bonus, cancel bonus, etc